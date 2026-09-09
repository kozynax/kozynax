using System.Collections.Generic;

namespace Kozynax.UI.Base
{
    public class Line
    {
        public Line(List<LineElement> lineElements, bool selected = false, Icons icons = Icons.No)
        {
            LineElements = lineElements;
            Selected = selected;
            Icons = icons;
        }

        public bool Selected { get; }
        public IReadOnlyList<LineElement> LineElements { get; }
        public Icons Icons { get; }
    }
}