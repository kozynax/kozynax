using System.Collections.Generic;
using Silk.NET.SDL;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.SdlBackend;

namespace ZXMAK2.Host.AvaloniaBackend
{
    /// <summary>
    /// SDL joystick backend without an SDL event loop: SDL_JoystickUpdate is
    /// pumped explicitly on every Scan (the Avalonia host never calls SDL_PumpEvents).
    /// </summary>
    public sealed class AvaloniaJoystick : IHostJoystick
    {
        private readonly Sdl _sdl;
        private readonly SdlJoystick _inner;

        public AvaloniaJoystick(Sdl sdl)
        {
            _sdl = sdl;
            _inner = new SdlJoystick(sdl);
            // Poll mode: state is read directly from the device on Scan.
            _sdl.JoystickEventState(0);
        }

        public IKeyboardState KeyboardState
        {
            set => _inner.KeyboardState = value;
        }

        public bool IsKeyboardStateRequired => _inner.IsKeyboardStateRequired;

        public void CaptureHostDevice(string hostId)
            => _inner.CaptureHostDevice(hostId);

        public void ReleaseHostDevice(string hostId)
            => _inner.ReleaseHostDevice(hostId);

        public void Scan()
        {
            _sdl.JoystickUpdate();
            _inner.Scan();
        }

        public IJoystickState GetState(string hostId)
            => _inner.GetState(hostId);

        public IEnumerable<IHostDeviceInfo> GetAvailableJoysticks()
            => _inner.GetAvailableJoysticks();

        public void Dispose()
            => _inner.Dispose();
    }
}
