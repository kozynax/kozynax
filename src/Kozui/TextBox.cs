using System;

namespace ZXMAK2.Host.WinForms.Lib
{
    /// <summary>
    /// Single-line editable text field for Kozui dialogs.
    /// </summary>
    public class TextBox : KozuiControl
    {
        private string _text = string.Empty;
        private int _maxLength = 64;

        public string Text
        {
            get => _text;
            set => SetProperty(ref _text, value ?? string.Empty);
        }

        public int MaxLength
        {
            get => _maxLength;
            set => SetProperty(ref _maxLength, Math.Max(1, value));
        }

        public void InsertChar(char ch)
        {
            if (ch < 32 || ch > 126)
                return;
            if (_text.Length >= _maxLength)
                return;
            Text = _text + ch;
        }

        public void Backspace()
        {
            if (_text.Length == 0)
                return;
            Text = _text.Substring(0, _text.Length - 1);
        }

        public void Clear()
            => Text = string.Empty;
    }
}
