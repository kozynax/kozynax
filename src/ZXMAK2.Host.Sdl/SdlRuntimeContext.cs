using System;
using Silk.NET.SDL;

namespace ZXMAK2.Host.SdlBackend
{
    /// <summary>
    /// Shared SDL window/renderer filled by <see cref="SdlMainView"/> after create.
    /// </summary>
    public sealed unsafe class SdlRuntimeContext
    {
        public Sdl Sdl { get; }
        public Window* Window { get; set; }
        public Renderer* Renderer { get; set; }

        /// <summary>Release emulator mouse capture for Terminal UI overlays.</summary>
        public Action PrepareUiInput { get; set; }

        /// <summary>Restore emulator mouse capture after Terminal UI overlays.</summary>
        public Action EndUiInput { get; set; }

        public SdlRuntimeContext(Sdl sdl)
        {
            Sdl = sdl;
        }

        public bool IsReady => Window != null && Renderer != null;
    }
}
