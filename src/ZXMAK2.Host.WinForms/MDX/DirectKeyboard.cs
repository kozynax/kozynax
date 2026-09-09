/* 
 *  Copyright 2008-2018 Alex Makeev
 * 
 *  This file is part of ZXMAK2 (ZX Spectrum virtual machine).
 *
 *  ZXMAK2 is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *
 *  ZXMAK2 is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with ZXMAK2.  If not, see <http://www.gnu.org/licenses/>.
 *
 *  Description: ZX Spectrum keyboard emulator
 *  Author: Alex Makeev
 *  Date: 26.03.2008, 10.07.2018
 */
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.Entities.Tools;
using ZXMAK2.Host.WinForms.Tools;
using ZXMAK2.DirectX;
using ZXMAK2.DirectX.DirectInput;
using ZxmakKey = ZXMAK2.Host.Entities.Key;
using MdxKey = ZXMAK2.DirectX.DirectInput.Key;


namespace ZXMAK2.Host.WinForms.Mdx
{
    public sealed class DirectKeyboard : IHostKeyboard, IKeyboardState
    {
        private readonly Form _form;
        private readonly IntPtr _hWnd;
        private DirectInputDevice8W _device;
        private KeyboardStateMapper<MdxKey> _mapper;
        private readonly Dictionary<ZxmakKey, bool> _state = new Dictionary<ZxmakKey, bool>();
        private bool _isAcquired;



        public DirectKeyboard(Form form)
        {
            if (form == null)
            {
                throw new ArgumentNullException("form");
            }
            _form = form;
            _hWnd = form.Handle;
            using (var dinput = new DirectInput8W())
            {
                _device = dinput.CreateDevice(SysGuid.GUID_SysKeyboard, null);
            }
            _device.SetDataFormat(DIDATAFORMAT.c_dfDIKeyboard).CheckError();
            _device.SetCooperativeLevel(_hWnd, DISCL.NONEXCLUSIVE | DISCL.FOREGROUND).CheckError();
            form.Deactivate += WndDeactivate;
            TryAcquire();
            _mapper = new KeyboardStateMapper<Key>(GetMapping());
        }

        public void Dispose()
        {
            if (_device == null)
            {
                return;
            }
            // TODO: sync needed
            var device = _device;
            _device = null;
            
            _form.Deactivate -= WndDeactivate;
            if (_isAcquired)
            {
                _isAcquired = false;
                var hr = device.Unacquire();
                if (hr.IsFailure)
                {
                    Logger.Error("DirectKeyboard.Dispose: {0}", hr);
                }
            }
            device.Dispose();
        }


        #region IKeyboardState

        public bool this[ZxmakKey key]
        {
            get { return _state.ContainsKey(key) && _state[key]; }
        }

        #endregion IKeyboardState


        #region IHostKeyboard

        public IKeyboardState State
        {
            get { return this; }
        }

        private byte[] _diState = new byte[256];
        public void Scan()
        {
            if (_device == null || (!_isAcquired && !TryAcquire()))
            {
                foreach (var key in _mapper.Keys)
                {
                    _state[key] = false;
                }
                return;
            }
            var hr = _device.GetDeviceState(_diState);
            if (hr.IsSuccess)
            {
                foreach (var key in _mapper.Keys)
                {
                    _state[key] = _diState[(int)_mapper[key]] != 0;
                }
            }
            else if (hr == ErrorCode.DIERR_NOTACQUIRED)
            {
                // TODO: sync needed
            }
            else if (hr == ErrorCode.DIERR_INPUTLOST)
            {
                WndDeactivate(null, null);
            }
            else
            {
                Logger.Error("DirectKeyboard.Scan: {0}", hr);
                WndDeactivate(null, null);
            }
        }

        #endregion IHostKeyboard


        #region Private

        private bool TryAcquire()
        {
            if (_device == null || 
                _hWnd != NativeMethods.GetForegroundWindow())
            {
                return false;
            }
            var hr = _device.Acquire();
            _isAcquired = true;
            if (hr.IsSuccess)
            {
                return true;
            }
            if (hr != ErrorCode.DIERR_OTHERAPPHASPRIO &&
                hr != ErrorCode.DIERR_INPUTLOST)
            {
                Logger.Error("DirectKeyboard.TryAcquire: {0}", hr);
            }
            return false;
        }

        private void WndDeactivate(object sender, EventArgs e)
        {
            if (_device == null || !_isAcquired)
            {
                return;
            }
            _isAcquired = false;
            var hr = _device.Unacquire();
            if (hr.IsSuccess)
            {
                return;
            }
            Logger.Error("DirectKeyboard.WndDeactivate: {0}", hr);
        }

        #endregion Private
        
        #region Mapping
        
        private Dictionary<ZxmakKey, Key> GetMapping()
        {
            return new Dictionary<ZxmakKey, Key>() {
                { ZxmakKey.D1, Key.D1 },
                { ZxmakKey.D2, Key.D2 },
                { ZxmakKey.D3, Key.D3 },
                { ZxmakKey.D4, Key.D4 },
                { ZxmakKey.D5, Key.D5 },
                { ZxmakKey.D6, Key.D6 },
                { ZxmakKey.D7, Key.D7 },
                { ZxmakKey.D8, Key.D8 },
                { ZxmakKey.D9, Key.D9 },
                { ZxmakKey.D0, Key.D0 },
                { ZxmakKey.Q, Key.Q },
                { ZxmakKey.W, Key.W },
                { ZxmakKey.E, Key.E },
                { ZxmakKey.R, Key.R },
                { ZxmakKey.T, Key.T },
                { ZxmakKey.Y, Key.Y },
                { ZxmakKey.U, Key.U },
                { ZxmakKey.I, Key.I },
                { ZxmakKey.O, Key.O },
                { ZxmakKey.P, Key.P },
                { ZxmakKey.A, Key.A },
                { ZxmakKey.S, Key.S },
                { ZxmakKey.D, Key.D },
                { ZxmakKey.F, Key.F },
                { ZxmakKey.G, Key.G },
                { ZxmakKey.H, Key.H },
                { ZxmakKey.J, Key.J },
                { ZxmakKey.K, Key.K },
                { ZxmakKey.L, Key.L },
                { ZxmakKey.Z, Key.Z },
                { ZxmakKey.X, Key.X },
                { ZxmakKey.C, Key.C },
                { ZxmakKey.V, Key.V },
                { ZxmakKey.B, Key.B },
                { ZxmakKey.N, Key.N },
                { ZxmakKey.M, Key.M },
                { ZxmakKey.Space, Key.Space },
                { ZxmakKey.Return, Key.Return },
                { ZxmakKey.F1, Key.F1 },
                { ZxmakKey.F2, Key.F2 },
                { ZxmakKey.F3, Key.F3 },
                { ZxmakKey.F4, Key.F4 },
                { ZxmakKey.F5, Key.F5 },
                { ZxmakKey.F6, Key.F6 },
                { ZxmakKey.F7, Key.F7 },
                { ZxmakKey.F8, Key.F8 },
                { ZxmakKey.F9, Key.F9 },
                { ZxmakKey.F10, Key.F10 },
                { ZxmakKey.F11, Key.F11 },
                { ZxmakKey.F12, Key.F12 },
                { ZxmakKey.F13, Key.F13 },
                { ZxmakKey.F14, Key.F14 },
                { ZxmakKey.F15, Key.F15 },
                { ZxmakKey.LeftShift, Key.LeftShift },
                { ZxmakKey.RightShift, Key.RightShift },
                { ZxmakKey.LeftAlt, Key.LeftAlt },
                { ZxmakKey.RightAlt, Key.RightAlt },
                { ZxmakKey.LeftControl, Key.LeftControl },
                { ZxmakKey.RightControl, Key.RightControl },
                { ZxmakKey.LeftWindows, Key.LeftWindows },
                { ZxmakKey.RightWindows, Key.RightWindows },
                { ZxmakKey.UpArrow, Key.UpArrow },
                { ZxmakKey.LeftArrow, Key.LeftArrow },
                { ZxmakKey.RightArrow, Key.RightArrow },
                { ZxmakKey.DownArrow, Key.DownArrow },
                { ZxmakKey.Insert, Key.Insert },
                { ZxmakKey.Delete, Key.Delete },
                { ZxmakKey.Home, Key.Home },
                { ZxmakKey.End, Key.End },
                { ZxmakKey.PageUp, Key.PageUp },
                { ZxmakKey.PageDown, Key.PageDown },
                { ZxmakKey.Escape, Key.Escape },
                { ZxmakKey.Tab, Key.Tab },
                { ZxmakKey.Minus, Key.Minus },
                { ZxmakKey.Equals, Key.Equals },
                { ZxmakKey.BackSpace, Key.BackSpace },
                { ZxmakKey.CapsLock, Key.CapsLock },
                { ZxmakKey.NumPadPlus, Key.NumPadPlus },
                { ZxmakKey.NumPadMinus, Key.NumPadMinus },
                { ZxmakKey.NumPadStar, Key.NumPadStar },
                { ZxmakKey.NumPadSlash, Key.NumPadSlash },
                { ZxmakKey.Period, Key.Period },
                { ZxmakKey.Comma, Key.Comma },
                { ZxmakKey.SemiColon, Key.SemiColon },
                { ZxmakKey.Apostrophe, Key.Apostrophe },
                { ZxmakKey.Slash, Key.Slash },
                { ZxmakKey.LeftBracket, Key.LeftBracket },
                { ZxmakKey.RightBracket, Key.RightBracket },
                { ZxmakKey.NumPadEnter, Key.NumPadEnter },
                { ZxmakKey.BackSlash, Key.BackSlash },
                { ZxmakKey.Grave, Key.Grave },
                { ZxmakKey.NumPad0, Key.NumPad0 },
                { ZxmakKey.NumPad1, Key.NumPad1 },
                { ZxmakKey.NumPad2, Key.NumPad2 },
                { ZxmakKey.NumPad3, Key.NumPad3 },
                { ZxmakKey.NumPad4, Key.NumPad4 },
                { ZxmakKey.NumPad5, Key.NumPad5 },
                { ZxmakKey.NumPad6, Key.NumPad6 },
                { ZxmakKey.NumPad7, Key.NumPad7 },
                { ZxmakKey.NumPad8, Key.NumPad8 },
                { ZxmakKey.NumPad9, Key.NumPad9 },
                { ZxmakKey.NumPadComma, Key.NumPadComma },
                { ZxmakKey.NumPadPeriod, Key.NumPadPeriod },
            };
        }
        
        #endregion
    }
}