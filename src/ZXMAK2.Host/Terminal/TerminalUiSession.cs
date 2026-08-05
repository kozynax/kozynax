using System;

namespace ZXMAK2.Host.Terminal
{
    /// <summary>
    /// Shared poll/render loop for Terminal-hosted Kozui dialogs.
    /// When <see cref="IdlePump"/> is set (stdio host), the SDL window keeps presenting.
    /// </summary>
    public static class TerminalUiSession
    {
        /// <summary>
        /// Optional host pump invoked after each frame (e.g. SDL ProcessEvents + PresentFrame).
        /// </summary>
        public static Action IdlePump { get; set; }

        /// <summary>Nested UI session depth (stdio PrepareForUiInput / NotifyEnter).</summary>
        public static int UiDepth { get; private set; }

        public static bool IsUiActive => UiDepth > 0;

        public static void NotifyEnter()
            => UiDepth++;

        public static void NotifyLeave()
        {
            if (UiDepth > 0)
                UiDepth--;
        }

        /// <summary>
        /// Returns true from <paramref name="onEvent"/> when the loop should stop processing further events this tick.
        /// </summary>
        public delegate bool EventHandler(TerminalEvent ev);

        public static void Run(
            ITerminal terminal,
            TerminalKozuiPresenter presenter,
            Func<bool> shouldExit,
            EventHandler onEvent,
            Action beforeRender = null)
        {
            if (terminal == null || presenter == null || shouldExit == null || onEvent == null)
                return;

            while (!shouldExit())
            {
                while (terminal.PollEvent(out var ev))
                {
                    if (onEvent(ev) || shouldExit())
                        break;
                }

                if (shouldExit())
                    break;

                beforeRender?.Invoke();
                presenter.MeasureArrangeFromTerminal();
                presenter.Render();
                IdlePump?.Invoke();
                terminal.Delay(16);
            }
        }

        /// <summary>
        /// For non-Kozui overlays (e.g. file picker) that draw themselves.
        /// </summary>
        public static void AfterFrame(ITerminal terminal)
        {
            IdlePump?.Invoke();
            terminal?.Delay(16);
        }
    }
}
