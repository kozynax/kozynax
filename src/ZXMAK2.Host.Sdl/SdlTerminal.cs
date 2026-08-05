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

        public SdlTerminal(SdlRuntimeContext runtime)
        {
            _runtime = runtime;
        }

        public override bool IsAvailable => _runtime.IsReady;

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

        public override void Clear(TerminalColor color)
        {
            var sdl = _runtime.Sdl;
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
                    case EventType.Keydown:
                        terminalEvent = TerminalEvent.KeyDown(MapKey(e.Key.Keysym));
                        return true;
                    case EventType.Keyup:
                        terminalEvent = TerminalEvent.KeyUp(MapKey(e.Key.Keysym));
                        return true;
                }
            }

            return false;
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
