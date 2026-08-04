namespace ZXMAK2.Host.WinForms.Lib
{
    public class Placeholder : KozuiControl
    {
        private KozuiControl _content;

        public KozuiControl Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }
    }
}