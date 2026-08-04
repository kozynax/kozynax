using System;

namespace ZXMAK2.Host.WinForms.Lib
{
    public class FileSelector : KozuiControl
    {
        public delegate void BrowseFileEventHandler(FileSelector fileSelector, string initialFileName);

        public event BrowseFileEventHandler OnBrowseFile;
        public event EventHandler FileSelected;

        private string _filter;
        private string _fileName;

        public string Filter
        {
            get => _filter;
            set => SetProperty(ref _filter, value);
        }

        public string FileName
        {
            get => _fileName;
            private set => SetProperty(ref _fileName, value);
        }

        public void SelectFile(string fileName)
        {
            FileName = fileName;
            FileSelected?.Invoke(this, EventArgs.Empty);
        }

        public void BrowseFile(string initialFileName)
        {
            OnBrowseFile?.Invoke(this, initialFileName);
        }
    }
}
