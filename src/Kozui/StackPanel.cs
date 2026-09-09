using ZXMAK2.Host.WinForms.Lib.Layout;

namespace ZXMAK2.Host.WinForms.Lib
{
    public class StackPanel : Panel
    {
        private Orientation _orientation = Orientation.Vertical;
        private int _spacing;

        public Orientation Orientation
        {
            get => _orientation;
            set => SetProperty(ref _orientation, value);
        }

        /// <summary>Gap between children, in cell units.</summary>
        public int Spacing
        {
            get => _spacing;
            set => SetProperty(ref _spacing, value);
        }
    }
}
