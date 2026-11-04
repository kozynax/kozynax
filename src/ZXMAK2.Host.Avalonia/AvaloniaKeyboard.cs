using System;
using System.Collections.Generic;
using Avalonia.Input;
using ZXMAK2.Host.Interfaces;
using ZxmakKey = ZXMAK2.Host.Entities.Key;

namespace ZXMAK2.Host.AvaloniaBackend
{
    /// <summary>
    /// Spectrum key matrix source fed from Avalonia key events (UI thread),
    /// read by the emulator thread via <see cref="IKeyboardState"/>.
    /// </summary>
    public sealed class AvaloniaKeyboard : IHostKeyboard, IKeyboardState
    {
        private readonly bool[] _state = new bool[Enum.GetValues(typeof(ZxmakKey)).Length];
        private readonly Dictionary<PhysicalKey, ZxmakKey> _map = BuildMapping();

        public IKeyboardState State => this;

        public bool this[ZxmakKey key]
        {
            get
            {
                var index = (int)key;
                return index >= 0 && index < _state.Length && _state[index];
            }
        }

        public void Scan()
        {
            // State is updated from Avalonia events on the UI thread.
        }

        public bool IsMapped(PhysicalKey key)
            => _map.ContainsKey(key);

        public void OnKeyEvent(PhysicalKey key, bool pressed)
        {
            if (_map.TryGetValue(key, out var zxKey))
                _state[(int)zxKey] = pressed;
        }

        public void Reset()
            => Array.Clear(_state, 0, _state.Length);

        public void Dispose()
        {
        }

        private static Dictionary<PhysicalKey, ZxmakKey> BuildMapping()
        {
            return new Dictionary<PhysicalKey, ZxmakKey>
            {
                { PhysicalKey.Digit1, ZxmakKey.D1 },
                { PhysicalKey.Digit2, ZxmakKey.D2 },
                { PhysicalKey.Digit3, ZxmakKey.D3 },
                { PhysicalKey.Digit4, ZxmakKey.D4 },
                { PhysicalKey.Digit5, ZxmakKey.D5 },
                { PhysicalKey.Digit6, ZxmakKey.D6 },
                { PhysicalKey.Digit7, ZxmakKey.D7 },
                { PhysicalKey.Digit8, ZxmakKey.D8 },
                { PhysicalKey.Digit9, ZxmakKey.D9 },
                { PhysicalKey.Digit0, ZxmakKey.D0 },
                { PhysicalKey.A, ZxmakKey.A },
                { PhysicalKey.B, ZxmakKey.B },
                { PhysicalKey.C, ZxmakKey.C },
                { PhysicalKey.D, ZxmakKey.D },
                { PhysicalKey.E, ZxmakKey.E },
                { PhysicalKey.F, ZxmakKey.F },
                { PhysicalKey.G, ZxmakKey.G },
                { PhysicalKey.H, ZxmakKey.H },
                { PhysicalKey.I, ZxmakKey.I },
                { PhysicalKey.J, ZxmakKey.J },
                { PhysicalKey.K, ZxmakKey.K },
                { PhysicalKey.L, ZxmakKey.L },
                { PhysicalKey.M, ZxmakKey.M },
                { PhysicalKey.N, ZxmakKey.N },
                { PhysicalKey.O, ZxmakKey.O },
                { PhysicalKey.P, ZxmakKey.P },
                { PhysicalKey.Q, ZxmakKey.Q },
                { PhysicalKey.R, ZxmakKey.R },
                { PhysicalKey.S, ZxmakKey.S },
                { PhysicalKey.T, ZxmakKey.T },
                { PhysicalKey.U, ZxmakKey.U },
                { PhysicalKey.V, ZxmakKey.V },
                { PhysicalKey.W, ZxmakKey.W },
                { PhysicalKey.X, ZxmakKey.X },
                { PhysicalKey.Y, ZxmakKey.Y },
                { PhysicalKey.Z, ZxmakKey.Z },
                { PhysicalKey.Space, ZxmakKey.Space },
                { PhysicalKey.Enter, ZxmakKey.Return },
                { PhysicalKey.F1, ZxmakKey.F1 },
                { PhysicalKey.F2, ZxmakKey.F2 },
                { PhysicalKey.F3, ZxmakKey.F3 },
                { PhysicalKey.F4, ZxmakKey.F4 },
                { PhysicalKey.F5, ZxmakKey.F5 },
                { PhysicalKey.F6, ZxmakKey.F6 },
                { PhysicalKey.F7, ZxmakKey.F7 },
                { PhysicalKey.F8, ZxmakKey.F8 },
                { PhysicalKey.F9, ZxmakKey.F9 },
                { PhysicalKey.F10, ZxmakKey.F10 },
                { PhysicalKey.F11, ZxmakKey.F11 },
                { PhysicalKey.F12, ZxmakKey.F12 },
                { PhysicalKey.F13, ZxmakKey.F13 },
                { PhysicalKey.F14, ZxmakKey.F14 },
                { PhysicalKey.F15, ZxmakKey.F15 },
                { PhysicalKey.ShiftLeft, ZxmakKey.LeftShift },
                { PhysicalKey.ShiftRight, ZxmakKey.RightShift },
                { PhysicalKey.AltLeft, ZxmakKey.LeftAlt },
                { PhysicalKey.AltRight, ZxmakKey.RightAlt },
                { PhysicalKey.ControlLeft, ZxmakKey.LeftControl },
                { PhysicalKey.ControlRight, ZxmakKey.RightControl },
                { PhysicalKey.MetaLeft, ZxmakKey.LeftWindows },
                { PhysicalKey.MetaRight, ZxmakKey.RightWindows },
                { PhysicalKey.ArrowUp, ZxmakKey.UpArrow },
                { PhysicalKey.ArrowLeft, ZxmakKey.LeftArrow },
                { PhysicalKey.ArrowRight, ZxmakKey.RightArrow },
                { PhysicalKey.ArrowDown, ZxmakKey.DownArrow },
                { PhysicalKey.Insert, ZxmakKey.Insert },
                { PhysicalKey.Delete, ZxmakKey.Delete },
                { PhysicalKey.Home, ZxmakKey.Home },
                { PhysicalKey.End, ZxmakKey.End },
                { PhysicalKey.PageUp, ZxmakKey.PageUp },
                { PhysicalKey.PageDown, ZxmakKey.PageDown },
                { PhysicalKey.Escape, ZxmakKey.Escape },
                { PhysicalKey.Tab, ZxmakKey.Tab },
                { PhysicalKey.Minus, ZxmakKey.Minus },
                { PhysicalKey.Equal, ZxmakKey.Equals },
                { PhysicalKey.Backspace, ZxmakKey.BackSpace },
                { PhysicalKey.CapsLock, ZxmakKey.CapsLock },
                { PhysicalKey.NumPadAdd, ZxmakKey.NumPadPlus },
                { PhysicalKey.NumPadSubtract, ZxmakKey.NumPadMinus },
                { PhysicalKey.NumPadMultiply, ZxmakKey.NumPadStar },
                { PhysicalKey.NumPadDivide, ZxmakKey.NumPadSlash },
                { PhysicalKey.Period, ZxmakKey.Period },
                { PhysicalKey.Comma, ZxmakKey.Comma },
                { PhysicalKey.Semicolon, ZxmakKey.SemiColon },
                { PhysicalKey.Quote, ZxmakKey.Apostrophe },
                { PhysicalKey.Slash, ZxmakKey.Slash },
                { PhysicalKey.BracketLeft, ZxmakKey.LeftBracket },
                { PhysicalKey.BracketRight, ZxmakKey.RightBracket },
                { PhysicalKey.NumPadEnter, ZxmakKey.NumPadEnter },
                { PhysicalKey.Backslash, ZxmakKey.BackSlash },
                { PhysicalKey.Backquote, ZxmakKey.Grave },
                { PhysicalKey.NumPad0, ZxmakKey.NumPad0 },
                { PhysicalKey.NumPad1, ZxmakKey.NumPad1 },
                { PhysicalKey.NumPad2, ZxmakKey.NumPad2 },
                { PhysicalKey.NumPad3, ZxmakKey.NumPad3 },
                { PhysicalKey.NumPad4, ZxmakKey.NumPad4 },
                { PhysicalKey.NumPad5, ZxmakKey.NumPad5 },
                { PhysicalKey.NumPad6, ZxmakKey.NumPad6 },
                { PhysicalKey.NumPad7, ZxmakKey.NumPad7 },
                { PhysicalKey.NumPad8, ZxmakKey.NumPad8 },
                { PhysicalKey.NumPad9, ZxmakKey.NumPad9 },
                { PhysicalKey.NumPadComma, ZxmakKey.NumPadComma },
                { PhysicalKey.NumPadDecimal, ZxmakKey.NumPadPeriod },
            };
        }
    }
}
