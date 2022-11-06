namespace Kozynax.UI.Base
{
    public class LineElement
    {
        public LineElement(string text, int firstCol, bool selected = false)
        {
            Text = text;
            FirstCol = firstCol;
            Selected = selected;
        }

        public bool Selected { get; }
        public string Text { get; }
        public int FirstCol { get; }
    }
}