using System;

namespace ZXMAK2.Host.WinForms.Lib
{
    public class CheckBox : KozuiControl
    {
        private bool _checked;
        private string _text;

        public event EventHandler CheckedStateChanged;

        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        public bool Checked
        {
            get => _checked;
            set
            {
                if (!SetProperty(ref _checked, value))
                    return;
                CheckedStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
