namespace ZXMAK2.Host.Terminal
{
    public readonly struct TerminalColor
    {
        public TerminalColor(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public byte R { get; }
        public byte G { get; }
        public byte B { get; }
        public byte A { get; }

        public static TerminalColor Rgb(byte r, byte g, byte b) => new TerminalColor(r, g, b);
    }
}
