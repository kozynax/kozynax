using System;
using System.Collections.Concurrent;
using Silk.NET.SDL;
using ZXMAK2.Host.Interfaces;
using Thread = System.Threading.Thread;
using ThreadPriority = System.Threading.ThreadPriority;
using AutoResetEvent = System.Threading.AutoResetEvent;
using WaitHandle = System.Threading.WaitHandle;

namespace ZXMAK2.Host.SdlBackend
{
    public sealed unsafe class SdlSound : IHostSound
    {
        private readonly Sdl _sdl;
        private readonly ConcurrentQueue<uint[]> _fillQueue = new ConcurrentQueue<uint[]>();
        private readonly ConcurrentQueue<uint[]> _playQueue = new ConcurrentQueue<uint[]>();
        private readonly AutoResetEvent _frameEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent _cancelEvent = new AutoResetEvent(false);
        private readonly int _sampleRate;
        private readonly int _bufferSize;
        private readonly int _bufferCount;
        private readonly uint _deviceId;
        private Thread _playThread;
        private volatile bool _isFinished;
        private uint? _lastSample;

        public SdlSound(Sdl sdl, int sampleRate = 44100, int bufferCount = 4)
        {
            if ((sampleRate % 50) != 0)
                throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be a multiple of 50!");

            _sdl = sdl;
            _sampleRate = sampleRate;
            _bufferSize = sampleRate / 50;
            _bufferCount = bufferCount;

            for (var i = 0; i < bufferCount; i++)
                _fillQueue.Enqueue(new uint[_bufferSize]);

            var want = new AudioSpec
            {
                Freq = sampleRate,
                Format = Sdl.AudioS16Lsb,
                Channels = 2,
                Samples = (ushort)_bufferSize,
            };

            AudioSpec have;
            _deviceId = _sdl.OpenAudioDevice((byte*)null, 0, &want, &have, 0);

            if (_deviceId == 0)
                throw new InvalidOperationException($"SDL OpenAudioDevice failed: {_sdl.GetErrorS()}");

            _playThread = new Thread(PlayThreadProc)
            {
                IsBackground = true,
                Name = "SdlWavePlay",
                Priority = ThreadPriority.Highest,
            };
            _playThread.Start();
            _sdl.PauseAudioDevice(_deviceId, 0);
        }

        public int SampleRate => _sampleRate;
        public bool IsSyncSupported => true;
        public bool IsSynchronized { get; set; }

        public void PushFrame(IFrameInfo info, IFrameSound frame)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));

            if (IsSynchronized)
                WaitFrame();

            if (!_fillQueue.TryDequeue(out var buffer))
                return;

            var src = frame.GetBuffer();
            Array.Copy(src, buffer, Math.Min(buffer.Length, src.Length));
            _playQueue.Enqueue(buffer);
        }

        public void CancelWait()
            => _cancelEvent.Set();

        private void WaitFrame()
        {
            _frameEvent.Reset();
            _cancelEvent.Reset();
            Thread.MemoryBarrier();
            if (_playQueue.Count == 0)
                return;
            WaitHandle.WaitAny(new WaitHandle[] { _frameEvent, _cancelEvent }, 40);
        }

        private void PlayThreadProc()
        {
            var bytesPerFrame = _bufferSize * 4;
            var silence = new byte[bytesPerFrame];

            try
            {
                while (!_isFinished)
                {
                    if (_playQueue.TryDequeue(out var source))
                    {
                        try
                        {
                            fixed (uint* pSrc = source)
                            {
                                _sdl.QueueAudio(_deviceId, pSrc, (uint)bytesPerFrame);
                                if (source.Length > 0)
                                    _lastSample = source[source.Length - 1];
                            }
                        }
                        finally
                        {
                            _fillQueue.Enqueue(source);
                            _frameEvent.Set();
                        }
                    }
                    else
                    {
                        // Keep device fed with last sample / silence to avoid underrun clicks.
                        if (_lastSample.HasValue)
                        {
                            var sample = _lastSample.Value;
                            fixed (byte* pSilence = silence)
                            {
                                var pUint = (uint*)pSilence;
                                for (var i = 0; i < _bufferSize; i++)
                                    pUint[i] = sample;
                                _sdl.QueueAudio(_deviceId, pSilence, (uint)bytesPerFrame);
                            }
                        }
                        else
                        {
                            fixed (byte* pSilence = silence)
                                _sdl.QueueAudio(_deviceId, pSilence, (uint)bytesPerFrame);
                        }
                    }

                    // Limit queued audio latency (~4 frames).
                    while (!_isFinished && _sdl.GetQueuedAudioSize(_deviceId) > (uint)(bytesPerFrame * _bufferCount))
                        Thread.Sleep(1);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public void Dispose()
        {
            if (_playThread == null)
                return;

            _isFinished = true;
            Thread.MemoryBarrier();
            _cancelEvent.Set();
            _playThread.Join(500);
            _playThread = null;

            if (_deviceId != 0)
            {
                _sdl.PauseAudioDevice(_deviceId, 1);
                _sdl.CloseAudioDevice(_deviceId);
            }

            _frameEvent.Dispose();
            _cancelEvent.Dispose();
        }
    }
}
