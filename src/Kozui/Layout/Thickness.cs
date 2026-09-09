namespace ZXMAK2.Host.WinForms.Lib.Layout
{
    /// <summary>Margin/padding in logical cell units.</summary>
    public readonly struct Thickness
    {
        public Thickness(int uniform)
            : this(uniform, uniform, uniform, uniform)
        {
        }

        public Thickness(int horizontal, int vertical)
            : this(horizontal, vertical, horizontal, vertical)
        {
        }

        public Thickness(int left, int top, int right, int bottom)
        {
            Left = left;
            Top = top;
            Right = right;
            Bottom = bottom;
        }

        public int Left { get; }
        public int Top { get; }
        public int Right { get; }
        public int Bottom { get; }

        public int Horizontal => Left + Right;
        public int Vertical => Top + Bottom;

        public static Thickness Zero => new Thickness(0);
    }
}
