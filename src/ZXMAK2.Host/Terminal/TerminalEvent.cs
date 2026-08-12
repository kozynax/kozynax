using System;

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

    [Flags]
    public enum TerminalKeyModifiers
    {
        None = 0,
        Ctrl = 1,
        Shift = 2,
        Alt = 4,
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
            char ch = '\0',
            TerminalKeyModifiers modifiers = TerminalKeyModifiers.None)
        {
            Kind = kind;
            Key = key;
            X = x;
            Y = y;
            Button = button;
            WheelDelta = wheelDelta;
            Char = ch;
            Modifiers = modifiers;
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
        public TerminalKeyModifiers Modifiers { get; }

        public bool Ctrl => (Modifiers & TerminalKeyModifiers.Ctrl) != 0;

        public static TerminalEvent QuitEvent => new TerminalEvent(TerminalEventKind.Quit);

        public static TerminalEvent KeyDown(
            TerminalKey key,
            char ch = '\0',
            TerminalKeyModifiers modifiers = TerminalKeyModifiers.None)
            => new TerminalEvent(TerminalEventKind.KeyDown, key, ch: ch, modifiers: modifiers);

        public static TerminalEvent KeyUp(
            TerminalKey key,
            TerminalKeyModifiers modifiers = TerminalKeyModifiers.None)
            => new TerminalEvent(TerminalEventKind.KeyUp, key, modifiers: modifiers);

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
