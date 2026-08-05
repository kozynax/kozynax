namespace ZXMAK2.Host.WinForms.Lib
{
    public class Placeholder : KozuiControl
    {
        private KozuiControl _content;

        public KozuiControl Content
        {
            get => _content;
            set
            {
                if (ReferenceEquals(_content, value))
                    return;
                if (_content != null)
                    _content.Parent = null;
                _content = value;
                if (_content != null)
                    _content.Parent = this;
                OnPropertyChanged();
            }
        }
    }
}