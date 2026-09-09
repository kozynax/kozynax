using System;

namespace ZXMAK2.Host.WinForms.Lib
{
    public class Button : KozuiControl
    {
        private string _text;

        public event EventHandler Clicked;

        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }

        public void Click(object sender, EventArgs args)
            => Clicked?.Invoke(sender, args);
    }
}
