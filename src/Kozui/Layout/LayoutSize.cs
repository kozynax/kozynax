namespace ZXMAK2.Host.WinForms.Lib.Layout
{
    /// <summary>Size in logical cell units.</summary>
    public readonly struct LayoutSize
    {
        public LayoutSize(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public int Width { get; }
        public int Height { get; }

        public static LayoutSize Empty => new LayoutSize(0, 0);
    }
}
