namespace Kozynax.UI
{
    /// <summary>
    /// SDL menu and debugger font steps stored in kozynax.ini.
    /// Regular is -1 and Small is -2 so existing FontSize values keep their look.
    /// The value is an offset on the window zoom, clamped to at least 1× when drawn.
    /// </summary>
    public static class UiFontSteps
    {
        public const int Smallest = -4;
        public const int Smaller = -3;
        public const int Small = -2;
        public const int Regular = -1;
        public const int Big = 0;
        public const int Biggest = 1;

        public static readonly string[] Names =
        {
            "Smallest",
            "Smaller",
            "Small",
            "Regular",
            "Big",
            "Biggest",
        };

        public static int Clamp(int offset)
        {
            if (offset < Smallest)
                return Smallest;
            if (offset > Biggest)
                return Biggest;
            return offset;
        }

        public static int ToIndex(int offset)
            => Clamp(offset) - Smallest;

        public static int FromIndex(int index)
        {
            if (index < 0)
                index = 0;
            if (index >= Names.Length)
                index = Names.Length - 1;
            return Smallest + index;
        }
    }
}
