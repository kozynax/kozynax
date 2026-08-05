namespace ZXMAK2.Host.WinForms.Lib.Layout
{
    /// <summary>Rectangle in logical cell units.</summary>
    public readonly struct LayoutRect
    {
        public LayoutRect(int x, int y, int width, int height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        public int X { get; }
        public int Y { get; }
        public int Width { get; }
        public int Height { get; }

        public int Right => X + Width;
        public int Bottom => Y + Height;

        public static LayoutRect Empty => new LayoutRect(0, 0, 0, 0);
    }
}
