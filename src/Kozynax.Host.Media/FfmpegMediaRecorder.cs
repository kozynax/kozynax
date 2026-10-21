using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using ZXMAK2;
using ZXMAK2.Host.Interfaces;

namespace Kozynax.Host.Media
{
    /// <summary>
    /// Records every pushed video/audio frame via an external ffmpeg binary.
    /// Video is encoded live from stdin; audio is written as raw PCM and muxed on stop.
    /// </summary>
    public sealed class FfmpegMediaRecorder : IMediaRecorder
    {
        private const int FrameRate = 50;

        private readonly string _outputPath;
        private readonly string _ffmpegPath;
        private readonly int _sampleRate;
        private readonly string _tempDir;
        private readonly string _tempVideoPath;
        private readonly string _tempAudioPath;
        private readonly ConcurrentQueue<MediaFrame> _queue = new ConcurrentQueue<MediaFrame>();
        private readonly AutoResetEvent _eventFrame = new AutoResetEvent(false);
        private readonly AutoResetEvent _eventCancel = new AutoResetEvent(false);
        private readonly object _startLock = new object();

        private int _width;
        private int _height;
        private Process _ffmpeg;
        private Stream _videoStdin;
        private FileStream _audioStream;
        private Thread _thread;
        private volatile bool _running;
        private Exception _fatalError;

        /// <summary>Set if encoding failed; checked after <see cref="Dispose"/>.</summary>
        public Exception Error => _fatalError;

        public FfmpegMediaRecorder(string outputPath, int width, int height, int sampleRate)
        {
            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path is required.", nameof(outputPath));
            if (sampleRate <= 0)
                throw new ArgumentOutOfRangeException(nameof(sampleRate));

            _ffmpegPath = FfmpegLocator.Find()
                ?? throw new FileNotFoundException(
                    "ffmpeg was not found. Install ffmpeg and ensure it is on PATH.");

            _outputPath = Path.GetFullPath(outputPath);
            _width = width;
            _height = height;
            _sampleRate = sampleRate;

            var outDir = Path.GetDirectoryName(_outputPath);
            if (!string.IsNullOrEmpty(outDir))
                Directory.CreateDirectory(outDir);

            _tempDir = Path.Combine(Path.GetTempPath(), "kozynax-record-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
            _tempVideoPath = Path.Combine(_tempDir, "video.mp4");
            _tempAudioPath = Path.Combine(_tempDir, "audio.pcm");

            _running = true;
            _thread = new Thread(RecordProc)
            {
                Name = "FfmpegRecord",
                IsBackground = true,
            };
            _thread.Start();
        }

        public string OutputPath => _outputPath;

        public void PushFrame(
            IFrameInfo infoFrame,
            IFrameVideo videoFrame,
            IFrameSound soundFrame)
        {
            if (!_running || videoFrame == null || soundFrame == null)
                return;

            var width = videoFrame.Size.Width;
            var height = videoFrame.Size.Height;
            if (width <= 0 || height <= 0)
                return;

            var image = new int[width * height];
            Array.Copy(videoFrame.Buffer, image, image.Length);

            var audioSrc = soundFrame.GetBuffer();
            var audio = new uint[audioSrc.Length];
            Array.Copy(audioSrc, audio, audio.Length);

            try
            {
                _queue.Enqueue(new MediaFrame
                {
                    Width = width,
                    Height = height,
                    Image = image,
                    Audio = audio,
                });
                _eventFrame.Set();
            }
            catch (ObjectDisposedException)
            {
                // Dispose raced with PushFrame
            }
        }

        public void Dispose()
        {
            if (!_running)
                return;

            _running = false;
            _eventCancel.Set();
            var thread = _thread;
            _thread = null;
            if (thread != null && thread.IsAlive)
                thread.Join();

            _eventFrame.Dispose();
            _eventCancel.Dispose();
        }

        private void RecordProc()
        {
            try
            {
                MediaFrame frame;
                while (_running)
                {
                    if (_queue.TryDequeue(out frame))
                    {
                        WriteFrame(frame);
                    }
                    else
                    {
                        WaitHandle.WaitAny(new WaitHandle[] { _eventFrame, _eventCancel });
                    }
                }

                while (_queue.TryDequeue(out frame))
                    WriteFrame(frame);

                FinishEncode();
            }
            catch (Exception ex)
            {
                _fatalError = ex;
                Logger.Error(ex, "FfmpegMediaRecorder failed");
                try
                {
                    AbortEncode();
                }
                catch (Exception abortEx)
                {
                    Logger.Error(abortEx);
                }
                TryDeleteDirectory(_tempDir);
            }
        }

        private void WriteFrame(MediaFrame frame)
        {
            EnsureStarted(frame.Width, frame.Height);

            if (frame.Width != _width || frame.Height != _height)
            {
                // Size changes mid-recording are not supported yet; keep A/V sync by
                // writing silence-sized audio and a black/copied letterbox would be
                // better later. For now duplicate via nearest pad into fixed buffer.
                frame = ResampleFrame(frame, _width, _height);
            }

            var videoBytes = new byte[frame.Image.Length * 4];
            Buffer.BlockCopy(frame.Image, 0, videoBytes, 0, videoBytes.Length);
            _videoStdin.Write(videoBytes, 0, videoBytes.Length);

            var audioBytes = new byte[frame.Audio.Length * 4];
            Buffer.BlockCopy(frame.Audio, 0, audioBytes, 0, audioBytes.Length);
            _audioStream.Write(audioBytes, 0, audioBytes.Length);
        }

        private void EnsureStarted(int width, int height)
        {
            if (_ffmpeg != null)
                return;

            lock (_startLock)
            {
                if (_ffmpeg != null)
                    return;

                if (_width <= 0 || _height <= 0)
                {
                    _width = width;
                    _height = height;
                }

                if (_width <= 0 || _height <= 0)
                    throw new InvalidOperationException("Invalid video frame size.");

                // yuv420p requires even dimensions.
                if ((_width & 1) != 0)
                    _width++;
                if ((_height & 1) != 0)
                    _height++;

                _audioStream = new FileStream(
                    _tempAudioPath,
                    FileMode.Create,
                    FileAccess.Write,
                    FileShare.Read,
                    64 * 1024,
                    FileOptions.SequentialScan);

                var args = new StringBuilder();
                args.Append("-loglevel error -y ");
                args.Append("-f rawvideo -pix_fmt bgra ");
                args.Append("-s ").Append(_width).Append('x').Append(_height).Append(' ');
                args.Append("-framerate ").Append(FrameRate).Append(' ');
                args.Append("-i pipe:0 ");
                args.Append("-an ");
                args.Append("-c:v libx264 -preset ultrafast -crf 18 -pix_fmt yuv420p ");
                QuotePath(args, _tempVideoPath);

                var psi = new ProcessStartInfo
                {
                    FileName = _ffmpegPath,
                    Arguments = args.ToString(),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true,
                };

                _ffmpeg = Process.Start(psi)
                    ?? throw new InvalidOperationException("Failed to start ffmpeg.");
                _videoStdin = _ffmpeg.StandardInput.BaseStream;
                BeginDrain(_ffmpeg.StandardError);
                BeginDrain(_ffmpeg.StandardOutput);
            }
        }

        private void FinishEncode()
        {
            if (_videoStdin != null)
            {
                try
                {
                    _videoStdin.Flush();
                    _videoStdin.Close();
                }
                catch (IOException)
                {
                    // ffmpeg may already have exited
                }
                _videoStdin = null;
            }

            if (_audioStream != null)
            {
                _audioStream.Flush();
                _audioStream.Dispose();
                _audioStream = null;
            }

            if (_ffmpeg != null)
            {
                if (!_ffmpeg.WaitForExit(120000))
                {
                    try { _ffmpeg.Kill(); } catch { /* ignore */ }
                    throw new TimeoutException("ffmpeg video encode timed out.");
                }
                if (_ffmpeg.ExitCode != 0)
                    throw new InvalidOperationException(
                        "ffmpeg video encode failed (exit " + _ffmpeg.ExitCode + ").");
                _ffmpeg.Dispose();
                _ffmpeg = null;
            }

            if (!File.Exists(_tempVideoPath) || !File.Exists(_tempAudioPath))
            {
                TryDeleteDirectory(_tempDir);
                return;
            }

            if (new FileInfo(_tempAudioPath).Length == 0)
            {
                File.Copy(_tempVideoPath, _outputPath, true);
                TryDeleteDirectory(_tempDir);
                return;
            }

            var muxArgs = new StringBuilder();
            muxArgs.Append("-loglevel error -y ");
            muxArgs.Append("-i ");
            QuotePath(muxArgs, _tempVideoPath);
            muxArgs.Append(' ');
            muxArgs.Append("-f s16le -ar ").Append(_sampleRate).Append(" -ac 2 -i ");
            QuotePath(muxArgs, _tempAudioPath);
            muxArgs.Append(' ');
            muxArgs.Append("-c:v copy -c:a aac -b:a 192k -shortest ");
            QuotePath(muxArgs, _outputPath);

            var muxPsi = new ProcessStartInfo
            {
                FileName = _ffmpegPath,
                Arguments = muxArgs.ToString(),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
            };

            using (var mux = Process.Start(muxPsi))
            {
                if (mux == null)
                    throw new InvalidOperationException("Failed to start ffmpeg mux.");
                var stderr = mux.StandardError.ReadToEnd();
                mux.StandardOutput.ReadToEnd();
                if (!mux.WaitForExit(120000))
                {
                    try { mux.Kill(); } catch { /* ignore */ }
                    throw new TimeoutException("ffmpeg mux timed out.");
                }
                if (mux.ExitCode != 0)
                    throw new InvalidOperationException(
                        "ffmpeg mux failed (exit " + mux.ExitCode + "): " + stderr);
            }

            TryDeleteDirectory(_tempDir);
        }

        private void AbortEncode()
        {
            try
            {
                if (_videoStdin != null)
                {
                    _videoStdin.Dispose();
                    _videoStdin = null;
                }
            }
            catch { /* ignore */ }

            try
            {
                if (_audioStream != null)
                {
                    _audioStream.Dispose();
                    _audioStream = null;
                }
            }
            catch { /* ignore */ }

            try
            {
                if (_ffmpeg != null && !_ffmpeg.HasExited)
                    _ffmpeg.Kill();
            }
            catch { /* ignore */ }

            try
            {
                _ffmpeg?.Dispose();
            }
            catch { /* ignore */ }
            _ffmpeg = null;
        }

        private static MediaFrame ResampleFrame(MediaFrame src, int width, int height)
        {
            var dst = new int[width * height];
            for (var y = 0; y < height; y++)
            {
                var sy = Math.Min(src.Height - 1, y * src.Height / height);
                for (var x = 0; x < width; x++)
                {
                    var sx = Math.Min(src.Width - 1, x * src.Width / width);
                    dst[y * width + x] = src.Image[sy * src.Width + sx];
                }
            }
            return new MediaFrame
            {
                Width = width,
                Height = height,
                Image = dst,
                Audio = src.Audio,
            };
        }

        private static void BeginDrain(StreamReader reader)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    while (reader.ReadLine() != null)
                    {
                        // discard
                    }
                }
                catch
                {
                    // process exited
                }
            });
        }

        private static void QuotePath(StringBuilder sb, string path)
        {
            sb.Append('"').Append(path.Replace("\"", "\\\"")).Append('"');
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to delete temp recording dir {0}", path);
            }
        }

        private sealed class MediaFrame
        {
            public int Width;
            public int Height;
            public int[] Image;
            public uint[] Audio;
        }
    }

    public static class FfmpegLocator
    {
        public static string Find()
        {
            var env = Environment.GetEnvironmentVariable("KOZYNAX_FFMPEG");
            if (!string.IsNullOrWhiteSpace(env) && File.Exists(env))
                return env;

            var local = Path.Combine(AppContext.BaseDirectory ?? ".",
                OperatingSystemIsWindows() ? "ffmpeg.exe" : "ffmpeg");
            if (File.Exists(local))
                return local;

            return FindOnPath(OperatingSystemIsWindows() ? "ffmpeg.exe" : "ffmpeg");
        }

        private static string FindOnPath(string fileName)
        {
            var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
            foreach (var dir in pathEnv.Split(Path.PathSeparator))
            {
                if (string.IsNullOrWhiteSpace(dir))
                    continue;
                var candidate = Path.Combine(dir.Trim('"', ' '), fileName);
                if (File.Exists(candidate))
                    return candidate;
            }
            return null;
        }

        private static bool OperatingSystemIsWindows()
        {
#if NETFRAMEWORK
            return true;
#else
            return System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                System.Runtime.InteropServices.OSPlatform.Windows);
#endif
        }
    }

    public static class VideoRecordingPaths
    {
        public static string GetDefaultVideosDirectory()
        {
            var videos = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos);
            if (string.IsNullOrWhiteSpace(videos))
            {
                var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                videos = Path.Combine(home, "Videos");
            }
            Directory.CreateDirectory(videos);
            return videos;
        }

        public static string CreateDefaultOutputPath()
        {
            var name = "kozynax_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".mp4";
            return Path.Combine(GetDefaultVideosDirectory(), name);
        }
    }
}
