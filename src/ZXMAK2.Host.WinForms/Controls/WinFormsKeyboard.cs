using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Entities.Tools;
using ZXMAK2.Host.Interfaces;

namespace ZXMAK2.Host.WinForms.Controls
{
    public sealed class WinFormsKeyboard : IHostKeyboard, IKeyboardState
    {
        private readonly Form _form;
        private readonly KeyboardStateMapper<Keys> _mapper;
        private readonly Dictionary<Key, bool> _state = new Dictionary<Key, bool>();

        public WinFormsKeyboard(Form form)
        {
            if (form == null)
            {
                throw new ArgumentNullException("form");
            }
            _form = form;
            _mapper = new KeyboardStateMapper<Keys>(GetMapping());
            foreach (var key in _mapper.Keys)
            {
                _state[key] = false;
            }
            _form.KeyDown += Form_OnKeyDown;
            _form.KeyUp += Form_OnKeyUp;
        }

        public void Dispose()
        {
        }


        #region IHostKeyboard

        public void Scan()
        {
        }

        public IKeyboardState State
        {
            get { return this; }
        }

        #endregion IHostKeyboard


        #region IKeyboardState

        public bool this[Key key]
        {
            get { return _state.ContainsKey(key) && _state[key]; }
        }

        #endregion IKeyboardState


        #region Private

        private void Form_OnKeyDown(object sender, KeyEventArgs e)
        {
            //Logger.Debug("KeyCode={0}, KeyValue={1}, Modifiers={2}", e.KeyCode, e.KeyValue, e.Modifiers);
            foreach (var key in _mapper.Keys.ToArray())
            {
                if (_mapper[key] == e.KeyCode)
                {
                    _state[key] = true;
                    //Logger.Debug("DN: {0}", key);
                }
            }
        }

        private void Form_OnKeyUp(object sender, KeyEventArgs e)
        {
            foreach (var key in _mapper.Keys.ToArray())
            {
                if (_mapper[key] == e.KeyCode)
                {
                    _state[key] = false;
                    //Logger.Debug("UP: {0}", key);
                }
            }
        }

        #endregion Private
        
        #region Mapping
        
        private Dictionary<Key, Keys> GetMapping()
        {
            return new Dictionary<Key, Keys>() {
                { Key.D1, Keys.D1 },
                { Key.D2, Keys.D2 },
                { Key.D3, Keys.D3 },
                { Key.D4, Keys.D4 },
                { Key.D5, Keys.D5 },
                { Key.D6, Keys.D6 },
                { Key.D7, Keys.D7 },
                { Key.D8, Keys.D8 },
                { Key.D9, Keys.D9 },
                { Key.D0, Keys.D0 },
                { Key.Q, Keys.Q },
                { Key.W, Keys.W },
                { Key.E, Keys.E },
                { Key.R, Keys.R },
                { Key.T, Keys.T },
                { Key.Y, Keys.Y },
                { Key.U, Keys.U },
                { Key.I, Keys.I },
                { Key.O, Keys.O },
                { Key.P, Keys.P },
                { Key.A, Keys.A },
                { Key.S, Keys.S },
                { Key.D, Keys.D },
                { Key.F, Keys.F },
                { Key.G, Keys.G },
                { Key.H, Keys.H },
                { Key.J, Keys.J },
                { Key.K, Keys.K },
                { Key.L, Keys.L },
                { Key.Z, Keys.Z },
                { Key.X, Keys.X },
                { Key.C, Keys.C },
                { Key.V, Keys.V },
                { Key.B, Keys.B },
                { Key.N, Keys.N },
                { Key.M, Keys.M },
                { Key.Space, Keys.Space },
                { Key.Return, Keys.Return },
                { Key.F1, Keys.F1 },
                { Key.F2, Keys.F2 },
                { Key.F3, Keys.F3 },
                { Key.F4, Keys.F4 },
                { Key.F5, Keys.F5 },
                { Key.F6, Keys.F6 },
                { Key.F7, Keys.F7 },
                { Key.F8, Keys.F8 },
                { Key.F9, Keys.F9 },
                { Key.F10, Keys.F10 },
                { Key.F11, Keys.F11 },
                { Key.F12, Keys.F12 },
                { Key.F13, Keys.F13 },
                { Key.F14, Keys.F14 },
                { Key.F15, Keys.F15 },
                { Key.LeftShift, Keys.ShiftKey },
                { Key.RightShift, Keys.ControlKey },
                { Key.LeftAlt, Keys.Menu },
                { Key.RightAlt, Keys.Menu },
                { Key.LeftControl, Keys.ControlKey },
                { Key.RightControl, Keys.ControlKey },
                { Key.LeftWindows, Keys.LWin },
                { Key.RightWindows, Keys.RWin },
                { Key.UpArrow, Keys.Up },
                { Key.LeftArrow, Keys.Left },
                { Key.RightArrow, Keys.Right },
                { Key.DownArrow, Keys.Down },
                { Key.Insert, Keys.Insert },
                { Key.Delete, Keys.Delete },
                { Key.Home, Keys.Home },
                { Key.End, Keys.End },
                { Key.PageUp, Keys.PageUp },
                { Key.PageDown, Keys.PageDown },
                { Key.Escape, Keys.Escape },
                { Key.Tab, Keys.Tab },
                { Key.Minus, Keys.OemMinus },
                { Key.Equals, Keys.Oemplus },
                { Key.BackSpace, Keys.Back },
                { Key.CapsLock, Keys.Capital },
                { Key.NumPadPlus, Keys.Add },
                { Key.NumPadMinus, Keys.Subtract },
                { Key.NumPadStar, Keys.Multiply },
                { Key.NumPadSlash, Keys.Divide },
                { Key.Period, Keys.OemPeriod },
                { Key.Comma, Keys.Oemcomma },
                { Key.SemiColon, Keys.Oem1 },
                { Key.Apostrophe, Keys.Oem7 },
                { Key.Slash, Keys.OemQuestion },
                { Key.LeftBracket, Keys.OemOpenBrackets },
                { Key.RightBracket, Keys.Oem6 },
                { Key.NumPadEnter, Keys.Return },
                { Key.BackSlash, Keys.Oem5 },
                { Key.Grave, Keys.Oemtilde },
                { Key.NumPad0, Keys.NumPad0 },
                { Key.NumPad1, Keys.NumPad1 },
                { Key.NumPad2, Keys.NumPad2 },
                { Key.NumPad3, Keys.NumPad3 },
                { Key.NumPad4, Keys.NumPad4 },
                { Key.NumPad5, Keys.NumPad5 },
                { Key.NumPad6, Keys.NumPad6 },
                { Key.NumPad7, Keys.NumPad7 },
                { Key.NumPad8, Keys.NumPad8 },
                { Key.NumPad9, Keys.NumPad9 },
                { Key.NumPadComma, Keys.Decimal },
                { Key.NumPadPeriod, Keys.Decimal },
            };
        }
        
        #endregion
    }
}
