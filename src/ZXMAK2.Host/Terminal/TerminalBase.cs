namespace ZXMAK2.Host.Terminal
{
    /// <summary>
    /// Optional base: default text drawing through <see cref="TerminalFont"/>.
    /// Hosts only need to implement clear/fill/present/input/size.
    /// </summary>
    public abstract class TerminalBase : ITerminal
    {
        public const int DefaultUiScale = 1;
 
        public abstract bool IsAvailable { get; }
        public abstract int Width { get; }
        public abstract int Height { get; }
        public virtual int UiScale => DefaultUiScale;

        public abstract void Clear(TerminalColor color);
        public abstract void FillRect(int x, int y, int width, int height, TerminalColor color);
        public abstract void Present();
        public abstract void Delay(int milliseconds);
        public virtual void CaptureBackdrop() { }
        public virtual void ReleaseBackdrop() { }
        public virtual bool HasBackdrop => false;
        public virtual void PrepareForUiInput() { }
        public virtual void EndUiInput() { }
        public abstract bool PollEvent(out TerminalEvent terminalEvent);

        public virtual void DrawText(int x, int y, string text, int scale, TerminalColor color)
            => TerminalFont.Draw(this, x, y, text, scale, color);

        public virtual int MeasureTextWidth(string text, int scale)
            => TerminalFont.MeasureWidth(text, scale);
    }
}
