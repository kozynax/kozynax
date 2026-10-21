using System;
using System.Diagnostics;
using System.IO;
using ZXMAK2.Host.Entities;
using Kozynax.Host.Media;
using Xunit;

namespace Kozynax.Tests
{
    public class FfmpegMediaRecorderTests
    {
        [Fact]
        public void RecordsVideoAndAudioFramesToMp4()
        {
            if (FfmpegLocator.Find() == null)
            {
                // CI / machines without ffmpeg: skip rather than fail.
                return;
            }

            var outPath = Path.Combine(
                Path.GetTempPath(),
                "kozynax-rec-test-" + Guid.NewGuid().ToString("N") + ".mp4");
            try
            {
                const int width = 32;
                const int height = 24;
                const int sampleRate = 44100;
                const int frames = 25; // 0.5s at 50Hz

                using (var recorder = new FfmpegMediaRecorder(outPath, width, height, sampleRate))
                {
                    var video = new FrameVideo(width, height, 1f);
                    for (var i = 0; i < video.Buffer.Length; i++)
                        video.Buffer[i] = unchecked((int)0xFF0000FF); // blue

                    var samplesPerFrame = (int)(sampleRate / 50.0 + 0.5);
                    var audioBuf = new uint[samplesPerFrame];
                    var sound = new FrameSound(sampleRate, new[] { audioBuf });
                    var info = new FrameInfo(Array.Empty<ZXMAK2.Host.Interfaces.IIconDescriptor>(), 0, 0, sampleRate, false);

                    for (var f = 0; f < frames; f++)
                    {
                        sound.Refresh();
                        recorder.PushFrame(info, video, sound);
                    }
                }

                Assert.True(File.Exists(outPath), "output mp4 missing");
                Assert.True(new FileInfo(outPath).Length > 0, "output mp4 empty");
            }
            finally
            {
                try { if (File.Exists(outPath)) File.Delete(outPath); } catch { /* ignore */ }
            }
        }

        [Fact]
        public void EmbedsSampleAspectRatioFromFrameRatio()
        {
            var ffmpeg = FfmpegLocator.Find();
            if (ffmpeg == null)
                return;

            var outPath = Path.Combine(
                Path.GetTempPath(),
                "kozynax-rec-sar-" + Guid.NewGuid().ToString("N") + ".mp4");
            try
            {
                // Profi-like: 64x32 buffer, ratio 2 → display 64x64, SAR 1:2
                const int width = 64;
                const int height = 32;
                const float ratio = 2f;
                const int sampleRate = 44100;

                using (var recorder = new FfmpegMediaRecorder(outPath, width, height, sampleRate))
                {
                    var video = new FrameVideo(width, height, ratio);
                    var samplesPerFrame = (int)(sampleRate / 50.0 + 0.5);
                    var sound = new FrameSound(sampleRate, new[] { new uint[samplesPerFrame] });
                    var info = new FrameInfo(Array.Empty<ZXMAK2.Host.Interfaces.IIconDescriptor>(), 0, 0, sampleRate, false);
                    for (var f = 0; f < 10; f++)
                    {
                        sound.Refresh();
                        recorder.PushFrame(info, video, sound);
                    }
                }

                Assert.True(File.Exists(outPath));

                var probe = ProbeSar(ffmpeg, outPath);
                Assert.False(string.IsNullOrEmpty(probe), "ffprobe produced no SAR");
                // Accept 1:2 or 0:1-style reduced forms that equal 0.5
                Assert.True(
                    probe == "1:2" || probe == "1/2" || probe.StartsWith("0.5"),
                    "unexpected SAR: " + probe);
            }
            finally
            {
                try { if (File.Exists(outPath)) File.Delete(outPath); } catch { /* ignore */ }
            }
        }

        private static string ProbeSar(string ffmpegPath, string mediaPath)
        {
            var ffprobe = Path.Combine(
                Path.GetDirectoryName(ffmpegPath) ?? string.Empty,
                Path.GetFileName(ffmpegPath).Replace("ffmpeg", "ffprobe"));
            if (!File.Exists(ffprobe))
                ffprobe = "ffprobe";

            var psi = new ProcessStartInfo
            {
                FileName = ffprobe,
                Arguments = "-v error -select_streams v:0 -show_entries stream=sample_aspect_ratio -of default=nw=1:nk=1 \"" + mediaPath + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
            };
            using (var p = Process.Start(psi))
            {
                if (p == null)
                    return null;
                var stdout = p.StandardOutput.ReadToEnd().Trim();
                p.WaitForExit(15000);
                return stdout;
            }
        }
    }
}
