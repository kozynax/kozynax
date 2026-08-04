using System;
using Silk.NET.SDL;
using ZXMAK2.Host.Interfaces;

namespace ZXMAK2.Host.SdlBackend
{
    public sealed class SdlMouse : IHostMouse
    {
        private readonly Sdl _sdl;
        private readonly MouseStateWrapper _state = new MouseStateWrapper();
        private bool _captured;

        public SdlMouse(Sdl sdl)
        {
            _sdl = sdl;
        }

        public IMouseState MouseState => _state;
        public bool IsCaptured => _captured;

        public void Scan()
        {
            // Updated from SDL events.
        }

        public void Capture()
        {
            _captured = true;
            _sdl.SetRelativeMouseMode(SdlBool.True);
        }

        public void Uncapture()
        {
            _captured = false;
            _sdl.SetRelativeMouseMode(SdlBool.False);
        }

        public void OnMouseMotion(int dx, int dy)
        {
            if (!_captured)
                return;
            _state.AddDelta(dx, dy);
        }

        public void OnMouseButton(byte button, bool pressed)
        {
            // SDL: 1=left, 2=middle, 3=right
            var bit = button switch
            {
                1 => 1,
                3 => 2,
                2 => 4,
                _ => 0,
            };
            if (bit == 0)
                return;

            if (pressed)
                _state.Buttons |= bit;
            else
                _state.Buttons &= ~bit;
        }

        public void Dispose()
        {
            Uncapture();
        }

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
