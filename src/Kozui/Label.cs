namespace ZXMAK2.Host.WinForms.Lib
{
    public class Label : KozuiControl
    {
        private string _text;

        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value);
        }
    }
}
