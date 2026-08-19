namespace ZXMAK2.Host.Terminal
{
    /// <summary>
    /// Optional per-dialog Terminal behavior for <c>TerminalKozuiDialogHost</c>
    /// (custom keys/mouse, layout fit, idle pump). Plain dialogs omit this.
    /// </summary>
    public interface IKozuiTerminalSession
    {
        /// <summary>When true, the host dialog loop exits.</summary>
        bool ShouldClose { get; }

        void OnSessionStart(object owner, TerminalKozuiPresenter presenter);

        /// <summary>Return true if the event was consumed.</summary>
        bool TryHandleEvent(TerminalKozuiPresenter presenter, TerminalEvent ev);

        void BeforeRender(TerminalKozuiPresenter presenter);

        void OnSessionEnd();
    }
}
