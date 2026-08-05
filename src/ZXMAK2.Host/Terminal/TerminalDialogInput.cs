using ZXMAK2.Host.WinForms.Lib.Presenters;

namespace ZXMAK2.Host.Terminal
{
    /// <summary>
    /// Shared event routing for Terminal-hosted Kozui dialogs.
    /// </summary>
    public static class TerminalDialogInput
    {
        /// <summary>
        /// Returns true when Escape was pressed (host should close/cancel).
        /// Otherwise routes keyboard/mouse input to the presenter when applicable.
        /// </summary>
        public static bool IsEscape(TerminalEvent ev)
            => ev.Kind == TerminalEventKind.KeyDown
               && TerminalKozuiPresenter.MapKey(ev.Key) == KozuiInputKey.Escape;

        public static bool Route(TerminalKozuiPresenter presenter, TerminalEvent ev)
        {
            if (presenter == null)
                return false;
            if (!TerminalKozuiPresenter.TryMapEvent(ev, out var input))
                return false;
            return presenter.RouteInput(input);
        }
    }
}
