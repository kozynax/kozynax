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
        /// </summary>
        public int ResolveUiScale()
        {
            return ClampScale(Math.Max(ResolveBufferScale(), ResolveDpiScale()));
        }

        /// <summary>
        /// Multiplier for the SDL window size (startup default and View → Size presets).
        /// Matches <see cref="ResolveUiScale"/> when the framebuffer is 1:1 with the window,
        /// and stays 1 when SDL already maps a logical window onto a denser buffer.
        /// </summary>
        public int ResolveWindowScale()
        {
            var buffer = ResolveBufferScale();
            var ui = ResolveUiScale();
            if (buffer <= TerminalBase.DefaultUiScale)
                return ui;
            return ClampScale(ui / buffer);
        }

        private int ResolveBufferScale()
        {
            if (!IsReady)
                return TerminalBase.DefaultUiScale;

            int winW, winH, outW, outH;
            Sdl.GetWindowSize(Window, &winW, &winH);
            Sdl.GetRendererOutputSize(Renderer, &outW, &outH);

            var scale = TerminalBase.DefaultUiScale;
            if (winW > 0 && outW > winW)
                scale = Math.Max(scale, (int)Math.Round(outW / (double)winW));
            if (winH > 0 && outH > winH)
                scale = Math.Max(scale, (int)Math.Round(outH / (double)winH));
            return ClampScale(scale);
        }

        private int ResolveDpiScale()
        {
            var displayIndex = 0;
            if (Window != null)
            {
                var index = Sdl.GetWindowDisplayIndex(Window);
                if (index >= 0)
                    displayIndex = index;
            }

            float ddpi = 0, hdpi = 0, vdpi = 0;
            if (Sdl.GetDisplayDPI(displayIndex, ref ddpi, ref hdpi, ref vdpi) != 0)
                return TerminalBase.DefaultUiScale;

            var dpi = Math.Max(ddpi, Math.Max(hdpi, vdpi));
            if (dpi <= BaselineDpi * 1.25f)
                return TerminalBase.DefaultUiScale;

            return ClampScale((int)Math.Round(dpi / BaselineDpi));
        }

        private static int ClampScale(int scale)
        {
            return Math.Min(MaxUiScale, Math.Max(TerminalBase.DefaultUiScale, scale));
        }

        public SdlMenuImagePainter CreateMenuImagePainter()
        {
            if (!IsReady)
                return null;
            return new SdlMenuImagePainter(Sdl, Renderer);
        }
    }
}
