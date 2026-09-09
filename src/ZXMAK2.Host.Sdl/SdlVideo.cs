using System;
using System.Drawing;
using System.Threading;
using ZXMAK2.Host.Interfaces;

namespace ZXMAK2.Host.SdlBackend
{
    public sealed class SdlVideo : IHostVideo
    {
        private static readonly IIconDescriptor[] NoIcons = Array.Empty<IIconDescriptor>();

        private readonly object _sync = new object();
        private readonly AutoResetEvent _frameEvent = new AutoResetEvent(false);
        private readonly AutoResetEvent _cancelEvent = new AutoResetEvent(false);

        private int[] _backBuffer;
        private int[] _frontBuffer;
        private Size _size;
        private float _ratio = 1f;
        private IIconDescriptor[] _icons = NoIcons;
        private bool _hasFrame;
        private bool _disposed;
        private int _startTact;
        private double _updateTime;
        private int _sampleRate;
        private bool _isRefresh;

        public bool IsSyncSupported => true;
        public bool IsSynchronized { get; set; }

        /// <summary>
        /// Latest icon descriptors from the emu (shared instances; read <see cref="IIconDescriptor.Visible"/> live).
        /// </summary>
        public IIconDescriptor[] Icons
        {
            get
            {
                lock (_sync)
                    return _icons ?? NoIcons;
            }
        }

        public void PushFrame(IFrameInfo info, IFrameVideo frame)
        {
            if (info == null)
                throw new ArgumentNullException(nameof(info));
            if (frame == null)
                throw new ArgumentNullException(nameof(frame));

            lock (_sync)
            {
                var src = frame.Buffer;
                var length = src.Length;
                if (_backBuffer == null || _backBuffer.Length != length)
                    _backBuffer = new int[length];
                Array.Copy(src, _backBuffer, length);
                _size = frame.Size;
                _ratio = frame.Ratio;
                _icons = info.Icons ?? NoIcons;
                _startTact = info.StartTact;
                _updateTime = info.UpdateTime;
                _sampleRate = info.SampleRate;
                _isRefresh = info.IsRefresh;
                _hasFrame = true;
            }

            if (IsSynchronized && !info.IsRefresh)
                WaitFrame();
        }

        public void CancelWait()
            => _cancelEvent.Set();

        public bool TryConsumeFrame(out int[] buffer, out Size size, out float ratio)
            => TryConsumeFrame(out buffer, out size, out ratio, out _);

        public bool TryConsumeFrame(
            out int[] buffer,
            out Size size,
            out float ratio,
            out SdlFrameDebugInfo debug)
        {
            lock (_sync)
            {
                if (!_hasFrame || _backBuffer == null)
                {
                    buffer = null;
                    size = Size.Empty;
                    ratio = 1f;
                    debug = default;
                    return false;
                }

                // Swap so the presenter owns a stable buffer while the emu fills the other.
                var tmp = _frontBuffer;
                _frontBuffer = _backBuffer;
                _backBuffer = tmp ?? new int[_frontBuffer.Length];
                if (_backBuffer.Length != _frontBuffer.Length)
                    _backBuffer = new int[_frontBuffer.Length];

                buffer = _frontBuffer;
                size = _size;
                ratio = _ratio;
                debug = new SdlFrameDebugInfo(_startTact, _updateTime, _sampleRate, _isRefresh);
                _hasFrame = false;
                return true;
            }
        }

        public void NotifyPresented()
            => _frameEvent.Set();

        private void WaitFrame()
        {
            _frameEvent.Reset();
            _cancelEvent.Reset();
            Thread.MemoryBarrier();
            WaitHandle.WaitAny(new WaitHandle[] { _frameEvent, _cancelEvent }, 40);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            CancelWait();
            _frameEvent.Dispose();
            _cancelEvent.Dispose();
        }
    }

    public readonly struct SdlFrameDebugInfo
    {
        public SdlFrameDebugInfo(int startTact, double updateTime, int sampleRate, bool isRefresh)
        {
            StartTact = startTact;
            UpdateTime = updateTime;
            SampleRate = sampleRate;
            IsRefresh = isRefresh;
        }

        public int StartTact { get; }
        public double UpdateTime { get; }
        public int SampleRate { get; }
        public bool IsRefresh { get; }
    }
}
