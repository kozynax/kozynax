using System;

namespace ZXMAK2.Host.WinForms.Lib
{
    public class CheckBox : KozuiControl
    {
        private bool _checked;
        public event EventHandler CheckedStateChanged;

        public bool Checked
        {
            get => _checked;
            set
            {
                _checked = value;
                CheckedStateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}