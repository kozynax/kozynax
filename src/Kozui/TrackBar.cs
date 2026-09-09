using System;

namespace ZXMAK2.Host.WinForms.Lib
{
    public class TrackBar : KozuiControl
    {
        private int _minimum;
        private int _maximum;
        private int _value;

        public event EventHandler ValueChanged;

        public int Minimum
        {
            get => _minimum;
            set => SetProperty(ref _minimum, value);
        }

        public int Maximum
        {
            get => _maximum;
            set => SetProperty(ref _maximum, value);
        }

        public int Value
        {
            get => _value;
            set
            {
                if (!SetProperty(ref _value, value))
                    return;
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
