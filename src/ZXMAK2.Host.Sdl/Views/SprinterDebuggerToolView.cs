using Kozynax.UI;
using ZXMAK2.Host.Presentation.Interfaces;
using ZXMAK2.Host.SdlBackend;
using ZXMAK2.Host.Terminal;

namespace ZXMAK2.Host.SdlBackend.Views
{
    /// <summary>
    /// SDL host for <see cref="SprinterDebuggerDialog"/> (MMU/ULA extended panel).
    /// </summary>
    public sealed class SprinterDebuggerToolView : DebuggerToolView, IDebuggerSprinterView
    {
        public SprinterDebuggerToolView(ITerminal terminal, SdlRuntimeContext runtime)
            : base(terminal, runtime)
        {
        }

        public void Init(SprinterDebuggerDialog ui)
            => Init((DebuggerDialog)ui);
    }
}
