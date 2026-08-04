using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ZXMAK2.Host.WinForms.Lib
{
    public class KozuiControl : INotifyPropertyChanged
    {
        private bool _enabled = true;
        private bool _visible = true;

        public event PropertyChangedEventHandler PropertyChanged;

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
