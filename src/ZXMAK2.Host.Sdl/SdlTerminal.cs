using Silk.NET.SDL;
using ZXMAK2.Host.Terminal;
using Event = Silk.NET.SDL.Event;

namespace ZXMAK2.Host.SdlBackend
{
    /// <summary>
    /// SDL implementation of <see cref="ITerminal"/>.
    /// </summary>
    public sealed unsafe class SdlTerminal : TerminalBase
    {
        private readonly SdlRuntimeContext _runtime;
        private Texture* _backdrop;
        private int _backdropW;
        private int _backdropH;
        private int _uiTextDepth;

        public SdlTerminal(SdlRuntimeContext runtime)
        {
            _runtime = runtime;
        }

        public override bool IsAvailable => _runtime.IsReady;

        public override bool HasBackdrop => _backdrop != null;

        public override int Width
        {
            get
            {
                if (!_runtime.IsReady)
                    return 0;
                int w, h;
                _runtime.Sdl.GetRendererOutputSize(_runtime.Renderer, &w, &h);
                return w;
            }
        }

        public override int Height
        {
            get
            {
                if (!_runtime.IsReady)
                    return 0;
                int w, h;
                _runtime.Sdl.GetRendererOutputSize(_runtime.Renderer, &w, &h);
                return h;
            }
        }

        public override void CaptureBackdrop()
        {
            if (!_runtime.IsReady)
                return;

            ReleaseBackdrop();
            var w = Width;
            var h = Height;
            if (w <= 0 || h <= 0)
                return;

            var sdl = _runtime.Sdl;
            var pixels = new byte[w * h * 4];
            fixed (byte* pPixels = pixels)
            {
                if (sdl.RenderReadPixels(
                        _runtime.Renderer,
                        null,
                        Sdl.PixelformatArgb8888,
                        pPixels,
                        w * 4) != 0)
                {
                    return;
                }

                _backdrop = sdl.CreateTexture(
                    _runtime.Renderer,
                    Sdl.PixelformatArgb8888,
                    (int)TextureAccess.Static,
                    w,
                    h);
                if (_backdrop == null)
                    return;

                sdl.UpdateTexture(_backdrop, null, pPixels, w * 4);
                _backdropW = w;
                _backdropH = h;
            }
        }

        public override void ReleaseBackdrop()
        {
            if (_backdrop == null || !_runtime.IsReady)
            {
                _backdrop = null;
                return;
            }
            _runtime.Sdl.DestroyTexture(_backdrop);
            _backdrop = null;
            _backdropW = 0;
            _backdropH = 0;
        }

        public override void Clear(TerminalColor color)
        {
            var sdl = _runtime.Sdl;
            if (_backdrop != null)
            {
                // Restore previous frame, then dim it for a modal look.
                var dst = new Silk.NET.Maths.Rectangle<int>(0, 0, Width, Height);
                var src = new Silk.NET.Maths.Rectangle<int>(0, 0, _backdropW, _backdropH);
                sdl.RenderCopy(_runtime.Renderer, _backdrop, &src, &dst);
                sdl.SetRenderDrawBlendMode(_runtime.Renderer, BlendMode.Blend);
                sdl.SetRenderDrawColor(_runtime.Renderer, 0, 0, 0, 160);
                var dim = new Silk.NET.Maths.Rectangle<int>(0, 0, Width, Height);
                sdl.RenderFillRect(_runtime.Renderer, &dim);
                sdl.SetRenderDrawBlendMode(_runtime.Renderer, BlendMode.None);
                return;
            }

            sdl.SetRenderDrawColor(_runtime.Renderer, color.R, color.G, color.B, color.A);
            sdl.RenderClear(_runtime.Renderer);
        }

        public override void FillRect(int x, int y, int width, int height, TerminalColor color)
        {
            var sdl = _runtime.Sdl;
            sdl.SetRenderDrawColor(_runtime.Renderer, color.R, color.G, color.B, color.A);
            var rect = new Silk.NET.Maths.Rectangle<int>(x, y, width, height);
            sdl.RenderFillRect(_runtime.Renderer, &rect);
        }

        public override void Present()
            => _runtime.Sdl.RenderPresent(_runtime.Renderer);

        public override void Delay(int milliseconds)
            => _runtime.Sdl.Delay((uint)System.Math.Max(0, milliseconds));

        public override void PrepareForUiInput()
        {
            if (!_runtime.IsReady)
                return;
            _runtime.PrepareUiInput?.Invoke();
            _runtime.Sdl.SetRelativeMouseMode(SdlBool.False);
            _runtime.Sdl.ShowCursor(1);
            if (_uiTextDepth++ == 0)
                _runtime.Sdl.StartTextInput();
        }

        public override void EndUiInput()
        {
            if (!_runtime.IsReady)
                return;
            if (_uiTextDepth > 0 && --_uiTextDepth == 0)
                _runtime.Sdl.StopTextInput();
            _runtime.EndUiInput?.Invoke();
        }

        public override bool PollEvent(out TerminalEvent terminalEvent)
        {
            terminalEvent = default;
            if (!_runtime.IsReady)
                return false;

            Event e;
            while (_runtime.Sdl.PollEvent(&e) != 0)
            {
                switch ((EventType)e.Type)
                {
                    case EventType.Quit:
                        terminalEvent = TerminalEvent.QuitEvent;
                        return true;
                    case EventType.Windowevent:
                        if ((WindowEventID)e.Window.Event == WindowEventID.Close)
                        {
                            terminalEvent = TerminalEvent.QuitEvent;
                            return true;
                        }
                        break;
                    case EventType.Textinput:
                    {
                        // Layout-aware characters (Shift+3 → '#', etc.).
                        // Space (32) is TerminalKey.Space on KeyDown — skip the TextInput duplicate.
                        var ch = (char)e.Text.Text[0];
                        if (ch > 32 && ch < 127)
                        {
                            terminalEvent = TerminalEvent.KeyDown(TerminalKey.Unknown, ch);
                            return true;
                        }
                        break;
                    }
                    case EventType.Keydown:
                    {
                        var key = MapKey(e.Key.Keysym);
                        var mods = MapModifiers();
                        // Printable chars come from TextInput while UI text mode is active
                        // (avoids wrong unshifted glyphs and double-inserts).
                        var ch = '\0';
                        if (_uiTextDepth <= 0)
                        {
                            var sym = (int)e.Key.Keysym.Sym;
                            if (sym >= 32 && sym < 127)
                                ch = (char)sym;
                        }
                        // Skip bare KeyDown for keys that TextInput will deliver (digits are
                        // Unknown; letters are mapped A–Z — both must wait for TextInput).
                        // Keep Ctrl/Alt+letter KeyDown (e.g. Ctrl+G) — TextInput does not follow.
                        if (_uiTextDepth > 0
                            && ch == '\0'
                            && (mods & (TerminalKeyModifiers.Ctrl | TerminalKeyModifiers.Alt)) == 0
                            && (key == TerminalKey.Unknown || IsLetterKey(key)))
                            break;
                        terminalEvent = TerminalEvent.KeyDown(key, ch, mods);
                        return true;
                    }
                    case EventType.Keyup:
                        terminalEvent = TerminalEvent.KeyUp(MapKey(e.Key.Keysym), MapModifiers());
                        return true;
                    case EventType.Mousebuttondown:
                    {
                        ScaleMouseToRenderer(e.Button.X, e.Button.Y, out var mx, out var my);
                        terminalEvent = TerminalEvent.MouseDown(mx, my, MapMouseButton(e.Button.Button));
                        return true;
                    }
                    case EventType.Mousebuttonup:
                    {
                        ScaleMouseToRenderer(e.Button.X, e.Button.Y, out var mx, out var my);
                        terminalEvent = TerminalEvent.MouseUp(mx, my, MapMouseButton(e.Button.Button));
                        return true;
                    }
                    case EventType.Mousemotion:
                    {
                        ScaleMouseToRenderer(e.Motion.X, e.Motion.Y, out var mx, out var my);
                        terminalEvent = TerminalEvent.MouseMove(mx, my);
                        return true;
                    }
                    case EventType.Mousewheel:
                    {
                        int mx, my;
                        _runtime.Sdl.GetMouseState(&mx, &my);
                        ScaleMouseToRenderer(mx, my, out var rx, out var ry);
                        // SDL wheel Y: positive away from user (up)
                        terminalEvent = TerminalEvent.MouseWheel(rx, ry, e.Wheel.Y);
                        return true;
                    }
                }
            }

            return false;
        }

        private void ScaleMouseToRenderer(int windowX, int windowY, out int rendererX, out int rendererY)
        {
            rendererX = windowX;
            rendererY = windowY;
            if (!_runtime.IsReady || _runtime.Window == null)
                return;

            int winW, winH;
            _runtime.Sdl.GetWindowSize(_runtime.Window, &winW, &winH);
            int outW = Width;
            int outH = Height;
            if (winW <= 0 || winH <= 0 || outW <= 0 || outH <= 0)
                return;
            if (winW == outW && winH == outH)
                return;

            rendererX = windowX * outW / winW;
            rendererY = windowY * outH / winH;
        }

        private TerminalKeyModifiers MapModifiers()
        {
            var mods = (Keymod)_runtime.Sdl.GetModState();
            var result = TerminalKeyModifiers.None;
            if ((mods & (Keymod.Ctrl | Keymod.Lctrl | Keymod.Rctrl)) != 0)
                result |= TerminalKeyModifiers.Ctrl;
            if ((mods & (Keymod.Shift | Keymod.Lshift | Keymod.Rshift)) != 0)
                result |= TerminalKeyModifiers.Shift;
            if ((mods & (Keymod.Alt | Keymod.Lalt | Keymod.Ralt)) != 0)
                result |= TerminalKeyModifiers.Alt;
            return result;
        }

        private static bool IsLetterKey(TerminalKey key)
            => key >= TerminalKey.A && key <= TerminalKey.Z;

        private static TerminalMouseButton MapMouseButton(byte button)
        {
            switch (button)
            {
                case 1: return TerminalMouseButton.Left;
                case 2: return TerminalMouseButton.Middle;
                case 3: return TerminalMouseButton.Right;
                default: return TerminalMouseButton.None;
            }
        }

        private static TerminalKey MapKey(Keysym keySym)
        {
            // Prefer scancodes for nav keys: some layouts/drivers report odd keycodes.
            switch (keySym.Scancode)
            {
                case Scancode.ScancodeBackspace:
                case Scancode.ScancodeDelete:
                case Scancode.ScancodeKPBackspace:
                    return TerminalKey.Backspace;
                case Scancode.ScancodePageup:
                    return TerminalKey.PageUp;
                case Scancode.ScancodePagedown:
                    return TerminalKey.PageDown;
                case Scancode.ScancodeUp:
                    return TerminalKey.Up;
                case Scancode.ScancodeDown:
                    return TerminalKey.Down;
                case Scancode.ScancodeLeft:
                    return TerminalKey.Left;
                case Scancode.ScancodeRight:
                    return TerminalKey.Right;
                case Scancode.ScancodeSpace:
                    return TerminalKey.Space;
            }

            var key = (KeyCode)keySym.Sym;
            switch (key)
            {
                case KeyCode.KEscape: return TerminalKey.Escape;
                case KeyCode.KReturn:
                case KeyCode.KKPEnter: return TerminalKey.Enter;
                case KeyCode.KBackspace:
                case KeyCode.KDelete: return TerminalKey.Backspace;
                case KeyCode.KUp: return TerminalKey.Up;
                case KeyCode.KDown: return TerminalKey.Down;
                case KeyCode.KLeft: return TerminalKey.Left;
                case KeyCode.KRight: return TerminalKey.Right;
                case KeyCode.KPageup:
                case KeyCode.KPrior: return TerminalKey.PageUp;
                case KeyCode.KPagedown: return TerminalKey.PageDown;
                case KeyCode.KTab: return TerminalKey.Tab;
                case KeyCode.KSpace: return TerminalKey.Space;
                case KeyCode.KF3: return TerminalKey.F3;
                case KeyCode.KF5: return TerminalKey.F5;
                case KeyCode.KF7: return TerminalKey.F7;
                case KeyCode.KF8: return TerminalKey.F8;
                case KeyCode.KF9: return TerminalKey.F9;
                case KeyCode.KA: return TerminalKey.A;
                case KeyCode.KB: return TerminalKey.B;
                case KeyCode.KC: return TerminalKey.C;
                case KeyCode.KD: return TerminalKey.D;
                case KeyCode.KE: return TerminalKey.E;
                case KeyCode.KF: return TerminalKey.F;
                case KeyCode.KG: return TerminalKey.G;
                case KeyCode.KH: return TerminalKey.H;
                case KeyCode.KI: return TerminalKey.I;
                case KeyCode.KJ: return TerminalKey.J;
                case KeyCode.KK: return TerminalKey.K;
                case KeyCode.KL: return TerminalKey.L;
                case KeyCode.KM: return TerminalKey.M;
                case KeyCode.KN: return TerminalKey.N;
                case KeyCode.KO: return TerminalKey.O;
                case KeyCode.KP: return TerminalKey.P;
                case KeyCode.KQ: return TerminalKey.Q;
                case KeyCode.KR: return TerminalKey.R;
                case KeyCode.KS: return TerminalKey.S;
                case KeyCode.KT: return TerminalKey.T;
                case KeyCode.KU: return TerminalKey.U;
                case KeyCode.KV: return TerminalKey.V;
                case KeyCode.KW: return TerminalKey.W;
                case KeyCode.KX: return TerminalKey.X;
                case KeyCode.KY: return TerminalKey.Y;
                case KeyCode.KZ: return TerminalKey.Z;
                default: return TerminalKey.Unknown;
            }
        }
    }
}
