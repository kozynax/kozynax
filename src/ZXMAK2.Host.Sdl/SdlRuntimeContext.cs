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
        private const int MaxUiScale = 4;

        public Sdl Sdl { get; }
        public Window* Window { get; set; }
        public Renderer* Renderer { get; set; }

        /// <summary>Emulator 100% pixel size (View → Size 100%).</summary>
        public int FrameWidth { get; private set; }

        public int FrameHeight { get; private set; }

        public float FrameRatio { get; private set; } = 1f;

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

        public void SetFrameSize(int width, int height, float ratio)
        {
            FrameWidth = Math.Max(0, width);
            FrameHeight = Math.Max(0, height);
            FrameRatio = ratio > 0 ? ratio : 1f;
        }

        /// <summary>
        /// Integer UI chrome scale from window size vs 100% frame size.
        /// Below 300% stays 1× (including 200%); 300%→2×, 400%→3×, 500%+→4×.
        /// </summary>
        public int ResolveUiScale()
        {
            if (!IsReady || FrameWidth <= 0 || FrameHeight <= 0)
                return TerminalBase.DefaultUiScale;

            int winW, winH;
            Sdl.GetWindowSize(Window, &winW, &winH);
            if (winW <= 0 || winH <= 0)
                return TerminalBase.DefaultUiScale;

            var baseW = FrameWidth;
            var baseH = Math.Max(1, (int)Math.Round(FrameHeight * FrameRatio));
            var n = Math.Min(winW / (double)baseW, winH / (double)baseH);
            var scale = (int)Math.Floor(n + 1e-6) - 1;
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
