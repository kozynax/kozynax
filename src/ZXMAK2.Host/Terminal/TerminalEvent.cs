namespace ZXMAK2.Host.Terminal
{
    public enum TerminalEventKind
    {
        None = 0,
        Quit,
        KeyDown,
        KeyUp,
        MouseDown,
        MouseUp,
        MouseMove,
        MouseWheel,
    }

    public enum TerminalMouseButton
    {
        None = 0,
        Left,
        Right,
        Middle,
    }

    public readonly struct TerminalEvent
    {
        public TerminalEvent(
            TerminalEventKind kind,
            TerminalKey key = TerminalKey.Unknown,
            int x = 0,
            int y = 0,
            TerminalMouseButton button = TerminalMouseButton.None,
            int wheelDelta = 0,
            char ch = '\0')
        {
            Kind = kind;
            Key = key;
            X = x;
            Y = y;
            Button = button;
            WheelDelta = wheelDelta;
            Char = ch;
        }

        public TerminalEventKind Kind { get; }
        public TerminalKey Key { get; }
        /// <summary>Pixel X in terminal/renderer coordinates.</summary>
        public int X { get; }
        /// <summary>Pixel Y in terminal/renderer coordinates.</summary>
        public int Y { get; }
        public TerminalMouseButton Button { get; }
        /// <summary>Positive = scroll up / away from user.</summary>
        public int WheelDelta { get; }
        /// <summary>Printable character when available (text input).</summary>
        public char Char { get; }

        public static TerminalEvent QuitEvent => new TerminalEvent(TerminalEventKind.Quit);

        public static TerminalEvent KeyDown(TerminalKey key, char ch = '\0')
            => new TerminalEvent(TerminalEventKind.KeyDown, key, ch: ch);

        public static TerminalEvent KeyUp(TerminalKey key)
            => new TerminalEvent(TerminalEventKind.KeyUp, key);

        public static TerminalEvent MouseDown(int x, int y, TerminalMouseButton button)
            => new TerminalEvent(TerminalEventKind.MouseDown, x: x, y: y, button: button);

        public static TerminalEvent MouseUp(int x, int y, TerminalMouseButton button)
            => new TerminalEvent(TerminalEventKind.MouseUp, x: x, y: y, button: button);

        public static TerminalEvent MouseMove(int x, int y)
            => new TerminalEvent(TerminalEventKind.MouseMove, x: x, y: y);

        public static TerminalEvent MouseWheel(int x, int y, int delta)
            => new TerminalEvent(TerminalEventKind.MouseWheel, x: x, y: y, wheelDelta: delta);
    }
}
