namespace ZXMAK2.Host.Terminal
{
    /// <summary>
    /// Backend-agnostic immediate-mode overlay surface.
    /// SDL / XNA / other hosts implement this; screens (file picker, dialogs) draw only here.
    /// Intended future host for Kozui control rendering (layout + widgets on top of this surface).
    /// </summary>
    public interface ITerminal
    {
        bool IsAvailable { get; }
        int Width { get; }
        int Height { get; }

        /// <summary>
        /// Integer multiplier for bitmap UI chrome (menus, dialogs, toolbar).
        /// Emulator video scale is independent. Default is 1.
        /// </summary>
        int UiScale { get; }

        void Clear(TerminalColor color);
        void FillRect(int x, int y, int width, int height, TerminalColor color);
        void DrawText(int x, int y, string text, int scale, TerminalColor color);
        int MeasureTextWidth(string text, int scale);

        void Present();
        void Delay(int milliseconds);

        /// <summary>
        /// Snapshot the current surface so a nested modal can draw over it
        /// instead of wiping the screen.
        /// </summary>
        void CaptureBackdrop();

        /// <summary>Drop the snapshot from <see cref="CaptureBackdrop"/>.</summary>
        void ReleaseBackdrop();

        /// <summary>True while a backdrop snapshot is active.</summary>
        bool HasBackdrop { get; }

        /// <summary>
        /// Release relative/captured mouse so overlays can use absolute clicks.
        /// </summary>
        void PrepareForUiInput();

        /// <summary>
        /// Restore emulator mouse capture after an overlay closes.
        /// </summary>
        void EndUiInput();

        /// <summary>
        /// Poll one input/window event. Returns false when the queue is empty.
        /// </summary>
        bool PollEvent(out TerminalEvent terminalEvent);
    }
}
