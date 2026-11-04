using System;
using Avalonia;
using ZXMAK2.Host.Interfaces;

namespace ZXMAK2.Host.AvaloniaBackend
{
    /// <summary>
    /// Kempston-style relative mouse built from Avalonia pointer events.
    /// Avalonia has no relative/raw pointer mode, so deltas are derived from
    /// absolute positions while captured; the cursor is hidden by the window.
    /// </summary>
    public sealed class AvaloniaMouse : IHostMouse
    {
        private readonly MouseStateWrapper _state = new MouseStateWrapper();
        private bool _captured;
        private Point? _lastPosition;

        /// <summary>Window-side hook: hide/show cursor, pointer capture.</summary>
        public event Action<bool> CaptureChanged;

        public IMouseState MouseState => _state;
        public bool IsCaptured => _captured;

        public void Scan()
        {
            // Updated from Avalonia pointer events.
        }

        public void Capture()
        {
            if (_captured)
                return;
            _captured = true;
            _lastPosition = null;
            CaptureChanged?.Invoke(true);
        }

        public void Uncapture()
        {
            if (!_captured)
                return;
            _captured = false;
            _lastPosition = null;
            _state.Buttons = 0;
            CaptureChanged?.Invoke(false);
        }

        public void OnPointerMoved(Point position)
        {
            if (!_captured)
                return;
            if (_lastPosition.HasValue)
            {
                var dx = (int)Math.Round(position.X - _lastPosition.Value.X);
                var dy = (int)Math.Round(position.Y - _lastPosition.Value.Y);
                if (dx != 0 || dy != 0)
                    _state.AddDelta(dx, dy);
            }
            _lastPosition = position;
        }

        /// <summary>Call after the host warped the pointer so the jump is not counted.</summary>
        public void ResetOrigin(Point position)
            => _lastPosition = position;

        public void OnButton(int bit, bool pressed)
        {
            if (bit == 0)
                return;
            if (pressed)
                _state.Buttons |= bit;
            else
                _state.Buttons &= ~bit;
        }

        public void Dispose()
            => Uncapture();

        private sealed class MouseStateWrapper : IMouseState
        {
            public int X { get; private set; }
            public int Y { get; private set; }
            public int Buttons { get; set; }

            public void AddDelta(int dx, int dy)
            {
                X += dx;
                Y += dy;
            }
        }
    }
}
