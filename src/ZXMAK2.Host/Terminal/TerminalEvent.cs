namespace ZXMAK2.Host.Terminal
{
    public enum TerminalEventKind
    {
        None = 0,
        Quit,
        KeyDown,
        KeyUp,
    }

    public readonly struct TerminalEvent
    {
        public TerminalEvent(TerminalEventKind kind, TerminalKey key = TerminalKey.Unknown)
        {
            Kind = kind;
            Key = key;
        }

        public TerminalEventKind Kind { get; }
        public TerminalKey Key { get; }

        public static TerminalEvent QuitEvent => new TerminalEvent(TerminalEventKind.Quit);
        public static TerminalEvent KeyDown(TerminalKey key) => new TerminalEvent(TerminalEventKind.KeyDown, key);
        public static TerminalEvent KeyUp(TerminalKey key) => new TerminalEvent(TerminalEventKind.KeyUp, key);
    }
}
