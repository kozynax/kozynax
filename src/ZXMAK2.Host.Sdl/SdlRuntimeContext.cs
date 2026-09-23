using System;
using Silk.NET.SDL;
using ZXMAK2.Host.Terminal;

namespace ZXMAK2.Host.SdlBackend
{
    /// <summary>
    /// Shared SDL window/renderer filled by <see cref="SdlMainView"/> after create.
    /// </summary>
    public sealed unsafe class SdlRuntimeContext
    {
        private const float BaselineDpi = 96f;
        private const int MaxUiScale = 4;

        public Sdl Sdl { get; }
        public Window* Window { get; set; }
        public Renderer* Renderer { get; set; }

        /// <summary>Release emulator mouse capture for Terminal UI overlays.</summary>
        public Action PrepareUiInput { get; set; }

        /// <summary>Restore emulator mouse capture after Terminal UI overlays.</summary>
        public Action EndUiInput { get; set; }

        /// <summary>
        /// Optional pump for stdio Kozui hosts so the SDL window keeps presenting
        /// while a console UI loop is blocking the main thread.
        /// </summary>
        public Action IdlePump { get; set; }

        public SdlRuntimeContext(Sdl sdl)
        {
            Sdl = sdl;
        }

        public bool IsReady => Window != null && Renderer != null;

        /// <summary>
        /// UI chrome scale from HiDPI buffer density and/or display DPI.
        /// Does not affect emulator video 2x/4x scaling.
        /// </summary>
        public int ResolveUiScale()
        {
            if (!IsReady)
                return TerminalBase.DefaultUiScale;

            int winW, winH, outW, outH;
            Sdl.GetWindowSize(Window, &winW, &winH);
            Sdl.GetRendererOutputSize(Renderer, &outW, &outH);

            var fromBuffer = TerminalBase.DefaultUiScale;
            if (winW > 0 && outW > winW)
                fromBuffer = Math.Max(fromBuffer, (int)Math.Round(outW / (double)winW));
            if (winH > 0 && outH > winH)
                fromBuffer = Math.Max(fromBuffer, (int)Math.Round(outH / (double)winH));

            var fromDpi = TerminalBase.DefaultUiScale;
            var displayIndex = Sdl.GetWindowDisplayIndex(Window);
            if (displayIndex >= 0)
            {
                float ddpi = 0, hdpi = 0, vdpi = 0;
                if (Sdl.GetDisplayDPI(displayIndex, ref ddpi, ref hdpi, ref vdpi) == 0)
                {
                    var dpi = Math.Max(ddpi, Math.Max(hdpi, vdpi));
                    if (dpi > BaselineDpi * 1.25f)
                        fromDpi = Math.Max(TerminalBase.DefaultUiScale, (int)Math.Round(dpi / BaselineDpi));
                }
            }

            return Math.Min(MaxUiScale, Math.Max(TerminalBase.DefaultUiScale, Math.Max(fromBuffer, fromDpi)));
        }

        public SdlMenuImagePainter CreateMenuImagePainter()
        {
            if (!IsReady)
                return null;
            return new SdlMenuImagePainter(Sdl, Renderer);
        }
    }
}
