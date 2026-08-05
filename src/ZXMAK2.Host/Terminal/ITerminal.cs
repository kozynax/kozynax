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

        void Clear(TerminalColor color);
        void FillRect(int x, int y, int width, int height, TerminalColor color);
        void DrawText(int x, int y, string text, int scale, TerminalColor color);
        int MeasureTextWidth(string text, int scale);

        void Present();
        void Delay(int milliseconds);

        /// <summary>
        /// Poll one input/window event. Returns false when the queue is empty.
        /// </summary>
        bool PollEvent(out TerminalEvent terminalEvent);
    }
}
