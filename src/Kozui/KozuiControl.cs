using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ZXMAK2.Host.WinForms.Lib.Layout;

namespace ZXMAK2.Host.WinForms.Lib
{
    public class KozuiControl : INotifyPropertyChanged
    {
        private bool _enabled = true;
        private bool _visible = true;
        private Thickness _margin;
        private HorizontalAlignment _horizontalAlignment = HorizontalAlignment.Stretch;
        private VerticalAlignment _verticalAlignment = VerticalAlignment.Stretch;
        private Dock _dock = Dock.None;
        private int _minWidth;
        private int _minHeight;
        private LayoutSize _desiredSize;
        private LayoutRect _arrangedBounds;

        public event PropertyChangedEventHandler PropertyChanged;

        public KozuiControl Parent { get; internal set; }

        public bool Enabled
        {
            get => _enabled;
            set => SetProperty(ref _enabled, value);
        }

        public bool Visible
        {
            get => _visible;
            set => SetProperty(ref _visible, value);
        }

        public Thickness Margin
        {
            get => _margin;
            set => SetProperty(ref _margin, value);
        }

        public HorizontalAlignment HorizontalAlignment
        {
            get => _horizontalAlignment;
            set => SetProperty(ref _horizontalAlignment, value);
        }

        public VerticalAlignment VerticalAlignment
        {
            get => _verticalAlignment;
            set => SetProperty(ref _verticalAlignment, value);
        }

        /// <summary>Dock edge when parent is <see cref="DockPanel"/>.</summary>
        public Dock Dock
        {
            get => _dock;
            set => SetProperty(ref _dock, value);
        }

        /// <summary>Minimum size in cell units (0 = no minimum).</summary>
        public int MinWidth
        {
            get => _minWidth;
            set => SetProperty(ref _minWidth, value);
        }

        public int MinHeight
        {
            get => _minHeight;
            set => SetProperty(ref _minHeight, value);
        }

        /// <summary>Result of the last measure pass (cell units).</summary>
        public LayoutSize DesiredSize
        {
            get => _desiredSize;
            internal set => _desiredSize = value;
        }

        /// <summary>Result of the last arrange pass (cell units).</summary>
        public LayoutRect ArrangedBounds
        {
            get => _arrangedBounds;
            internal set => _arrangedBounds = value;
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
