using System.Collections.Generic;
using Silk.NET.SDL;
using ZXMAK2.Host.Interfaces;
using ZxmakKey = ZXMAK2.Host.Entities.Key;

namespace ZXMAK2.Host.SdlBackend
{
    public sealed class SdlKeyboard : IHostKeyboard, IKeyboardState
    {
        private readonly Dictionary<ZxmakKey, bool> _state = new Dictionary<ZxmakKey, bool>();
        private readonly Dictionary<KeyCode, ZxmakKey> _map;

        public SdlKeyboard()
        {
            _map = BuildMapping();
            foreach (var key in _map.Values)
                _state[key] = false;
        }

        public IKeyboardState State => this;

        public bool this[ZxmakKey key]
            => _state.TryGetValue(key, out var pressed) && pressed;

        public void Scan()
        {
            // State is updated from SDL events on the UI thread.
        }

        public void OnKeyEvent(KeyCode keyCode, bool pressed)
        {
            if (_map.TryGetValue(keyCode, out var key))
                _state[key] = pressed;
        }

        public void Reset()
        {
            var keys = new List<ZxmakKey>(_state.Keys);
            foreach (var key in keys)
                _state[key] = false;
        }

        public void Dispose()
        {
        }

        private static Dictionary<KeyCode, ZxmakKey> BuildMapping()
        {
            return new Dictionary<KeyCode, ZxmakKey>
            {
                { KeyCode.K1, ZxmakKey.D1 },
                { KeyCode.K2, ZxmakKey.D2 },
                { KeyCode.K3, ZxmakKey.D3 },
                { KeyCode.K4, ZxmakKey.D4 },
                { KeyCode.K5, ZxmakKey.D5 },
                { KeyCode.K6, ZxmakKey.D6 },
                { KeyCode.K7, ZxmakKey.D7 },
                { KeyCode.K8, ZxmakKey.D8 },
                { KeyCode.K9, ZxmakKey.D9 },
                { KeyCode.K0, ZxmakKey.D0 },
                { KeyCode.KA, ZxmakKey.A },
                { KeyCode.KB, ZxmakKey.B },
                { KeyCode.KC, ZxmakKey.C },
                { KeyCode.KD, ZxmakKey.D },
                { KeyCode.KE, ZxmakKey.E },
                { KeyCode.KF, ZxmakKey.F },
                { KeyCode.KG, ZxmakKey.G },
                { KeyCode.KH, ZxmakKey.H },
                { KeyCode.KI, ZxmakKey.I },
                { KeyCode.KJ, ZxmakKey.J },
                { KeyCode.KK, ZxmakKey.K },
                { KeyCode.KL, ZxmakKey.L },
                { KeyCode.KM, ZxmakKey.M },
                { KeyCode.KN, ZxmakKey.N },
                { KeyCode.KO, ZxmakKey.O },
                { KeyCode.KP, ZxmakKey.P },
                { KeyCode.KQ, ZxmakKey.Q },
                { KeyCode.KR, ZxmakKey.R },
                { KeyCode.KS, ZxmakKey.S },
                { KeyCode.KT, ZxmakKey.T },
                { KeyCode.KU, ZxmakKey.U },
                { KeyCode.KV, ZxmakKey.V },
                { KeyCode.KW, ZxmakKey.W },
                { KeyCode.KX, ZxmakKey.X },
                { KeyCode.KY, ZxmakKey.Y },
                { KeyCode.KZ, ZxmakKey.Z },
                { KeyCode.KSpace, ZxmakKey.Space },
                { KeyCode.KReturn, ZxmakKey.Return },
                { KeyCode.KF1, ZxmakKey.F1 },
                { KeyCode.KF2, ZxmakKey.F2 },
                { KeyCode.KF3, ZxmakKey.F3 },
                { KeyCode.KF4, ZxmakKey.F4 },
                { KeyCode.KF5, ZxmakKey.F5 },
                { KeyCode.KF6, ZxmakKey.F6 },
                { KeyCode.KF7, ZxmakKey.F7 },
                { KeyCode.KF8, ZxmakKey.F8 },
                { KeyCode.KF9, ZxmakKey.F9 },
                { KeyCode.KF10, ZxmakKey.F10 },
                { KeyCode.KF11, ZxmakKey.F11 },
                { KeyCode.KF12, ZxmakKey.F12 },
                { KeyCode.KF13, ZxmakKey.F13 },
                { KeyCode.KF14, ZxmakKey.F14 },
                { KeyCode.KF15, ZxmakKey.F15 },
                { KeyCode.KLshift, ZxmakKey.LeftShift },
                { KeyCode.KRshift, ZxmakKey.RightShift },
                { KeyCode.KLalt, ZxmakKey.LeftAlt },
                { KeyCode.KRalt, ZxmakKey.RightAlt },
                { KeyCode.KLctrl, ZxmakKey.LeftControl },
                { KeyCode.KRctrl, ZxmakKey.RightControl },
                { KeyCode.KLgui, ZxmakKey.LeftWindows },
                { KeyCode.KRgui, ZxmakKey.RightWindows },
                { KeyCode.KUp, ZxmakKey.UpArrow },
                { KeyCode.KLeft, ZxmakKey.LeftArrow },
                { KeyCode.KRight, ZxmakKey.RightArrow },
                { KeyCode.KDown, ZxmakKey.DownArrow },
                { KeyCode.KInsert, ZxmakKey.Insert },
                { KeyCode.KDelete, ZxmakKey.Delete },
                { KeyCode.KHome, ZxmakKey.Home },
                { KeyCode.KEnd, ZxmakKey.End },
                { KeyCode.KPageup, ZxmakKey.PageUp },
                { KeyCode.KPagedown, ZxmakKey.PageDown },
                { KeyCode.KEscape, ZxmakKey.Escape },
                { KeyCode.KTab, ZxmakKey.Tab },
                { KeyCode.KMinus, ZxmakKey.Minus },
                { KeyCode.KEquals, ZxmakKey.Equals },
                { KeyCode.KBackspace, ZxmakKey.BackSpace },
                { KeyCode.KCapslock, ZxmakKey.CapsLock },
                { KeyCode.KKPPlus, ZxmakKey.NumPadPlus },
                { KeyCode.KKPMinus, ZxmakKey.NumPadMinus },
                { KeyCode.KKPMultiply, ZxmakKey.NumPadStar },
                { KeyCode.KKPDivide, ZxmakKey.NumPadSlash },
                { KeyCode.KPeriod, ZxmakKey.Period },
                { KeyCode.KComma, ZxmakKey.Comma },
                { KeyCode.KSemicolon, ZxmakKey.SemiColon },
                { KeyCode.KQuote, ZxmakKey.Apostrophe },
                { KeyCode.KSlash, ZxmakKey.Slash },
                { KeyCode.KLeftbracket, ZxmakKey.LeftBracket },
                { KeyCode.KRightbracket, ZxmakKey.RightBracket },
                { KeyCode.KKPEnter, ZxmakKey.NumPadEnter },
                { KeyCode.KBackslash, ZxmakKey.BackSlash },
                { KeyCode.KBackquote, ZxmakKey.Grave },
                { KeyCode.KKP0, ZxmakKey.NumPad0 },
                { KeyCode.KKP1, ZxmakKey.NumPad1 },
                { KeyCode.KKP2, ZxmakKey.NumPad2 },
                { KeyCode.KKP3, ZxmakKey.NumPad3 },
                { KeyCode.KKP4, ZxmakKey.NumPad4 },
                { KeyCode.KKP5, ZxmakKey.NumPad5 },
                { KeyCode.KKP6, ZxmakKey.NumPad6 },
                { KeyCode.KKP7, ZxmakKey.NumPad7 },
                { KeyCode.KKP8, ZxmakKey.NumPad8 },
                { KeyCode.KKP9, ZxmakKey.NumPad9 },
                { KeyCode.KKPComma, ZxmakKey.NumPadComma },
                { KeyCode.KKPPeriod, ZxmakKey.NumPadPeriod },
            };
        }
    }
}
