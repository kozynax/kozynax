using System;
using System.Collections.Generic;
using System.Linq;
using Silk.NET.SDL;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Interfaces;
using ZxmakKey = ZXMAK2.Host.Entities.Key;

namespace ZXMAK2.Host.SdlBackend
{
    public sealed unsafe class SdlJoystick : IHostJoystick
    {
        private const string KeyboardNumpadId = "keyboard";

        private readonly Sdl _sdl;
        private readonly Dictionary<string, IntPtr> _devices = new Dictionary<string, IntPtr>();
        private readonly Dictionary<string, IJoystickState> _states = new Dictionary<string, IJoystickState>();
        private IJoystickState _numpadState = StateWrapper.Empty;

        public SdlJoystick(Sdl sdl)
        {
            _sdl = sdl;
            _sdl.JoystickEventState(1);
        }

        public IKeyboardState KeyboardState { get; set; }
        public bool IsKeyboardStateRequired { get; private set; }

        public void CaptureHostDevice(string hostId)
        {
            if (string.IsNullOrEmpty(hostId))
                return;

            if (hostId == KeyboardNumpadId)
            {
                IsKeyboardStateRequired = true;
                return;
            }

            if (_devices.ContainsKey(hostId))
                return;

            var count = _sdl.NumJoysticks();
            for (var i = 0; i < count; i++)
            {
                var id = GetDeviceId(i);
                if (!string.Equals(id, hostId, StringComparison.OrdinalIgnoreCase))
                    continue;

                var joystick = _sdl.JoystickOpen(i);
                if (joystick == null)
                    continue;

                _devices[hostId] = (IntPtr)joystick;
                _states[hostId] = StateWrapper.Empty;
                return;
            }
        }

        public void ReleaseHostDevice(string hostId)
        {
            if (hostId == KeyboardNumpadId)
            {
                IsKeyboardStateRequired = false;
                return;
            }

            if (!_devices.TryGetValue(hostId, out var ptr))
                return;

            _sdl.JoystickClose((Joystick*)ptr);
            _devices.Remove(hostId);
            _states.Remove(hostId);
        }

        public void Scan()
        {
            foreach (var pair in _devices.ToList())
            {
                var joystick = (Joystick*)pair.Value;
                if (joystick == null)
                    continue;

                var x = _sdl.JoystickGetAxis(joystick, 0);
                var y = _sdl.JoystickGetAxis(joystick, 1);
                var fire = false;
                var buttons = _sdl.JoystickNumButtons(joystick);
                for (var b = 0; b < buttons; b++)
                {
                    if (_sdl.JoystickGetButton(joystick, b) != 0)
                    {
                        fire = true;
                        break;
                    }
                }

                const short deadZone = 8000;
                _states[pair.Key] = new StateWrapper(
                    x < -deadZone,
                    x > deadZone,
                    y < -deadZone,
                    y > deadZone,
                    fire);
            }

            if (IsKeyboardStateRequired && KeyboardState != null)
            {
                _numpadState = new StateWrapper(
                    KeyboardState[ZxmakKey.NumPad4],
                    KeyboardState[ZxmakKey.NumPad6],
                    KeyboardState[ZxmakKey.NumPad8],
                    KeyboardState[ZxmakKey.NumPad2],
                    KeyboardState[ZxmakKey.NumPad5] || KeyboardState[ZxmakKey.NumPad0]);
            }
        }

        public IJoystickState GetState(string hostId)
        {
            if (hostId == KeyboardNumpadId)
                return _numpadState;
            return _states.TryGetValue(hostId, out var state) ? state : StateWrapper.Empty;
        }

        public IEnumerable<IHostDeviceInfo> GetAvailableJoysticks()
        {
            var list = new List<IHostDeviceInfo>
            {
                new HostDeviceInfo(KeyboardNumpadId, "Keyboard Numpad"),
            };

            var count = _sdl.NumJoysticks();
            for (var i = 0; i < count; i++)
            {
                var namePtr = _sdl.JoystickNameForIndex(i);
                var name = namePtr != null
                    ? Silk.NET.Core.Native.SilkMarshal.PtrToString((IntPtr)namePtr)
                    : $"Joystick {i}";
                list.Add(new HostDeviceInfo(GetDeviceId(i), name));
            }

            return list;
        }

        public void Dispose()
        {
            foreach (var hostId in _devices.Keys.ToList())
                ReleaseHostDevice(hostId);
        }

        private string GetDeviceId(int index)
        {
            var guid = _sdl.JoystickGetDeviceGUID(index);
            return $"{guid.Data[0]:x2}{guid.Data[1]:x2}{guid.Data[2]:x2}{guid.Data[3]:x2}-" +
                   $"{guid.Data[4]:x2}{guid.Data[5]:x2}-" +
                   $"{guid.Data[6]:x2}{guid.Data[7]:x2}-" +
                   $"{guid.Data[8]:x2}{guid.Data[9]:x2}-" +
                   $"{guid.Data[10]:x2}{guid.Data[11]:x2}{guid.Data[12]:x2}{guid.Data[13]:x2}{guid.Data[14]:x2}{guid.Data[15]:x2}";
        }

        private sealed class StateWrapper : IJoystickState
        {
            public static readonly StateWrapper Empty = new StateWrapper(false, false, false, false, false);

            public StateWrapper(bool left, bool right, bool up, bool down, bool fire)
            {
                IsLeft = left;
                IsRight = right;
                IsUp = up;
                IsDown = down;
                IsFire = fire;
            }

            public bool IsLeft { get; }
            public bool IsRight { get; }
            public bool IsUp { get; }
            public bool IsDown { get; }
            public bool IsFire { get; }
        }
    }
}
