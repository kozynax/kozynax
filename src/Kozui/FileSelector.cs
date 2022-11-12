using System;

namespace ZXMAK2.Host.WinForms.Lib
{
    public class FileSelector : KozuiControl
    {
        public delegate void BrowseFileEventHandler(FileSelector fileSelector, string initialFileName);

        public event BrowseFileEventHandler OnBrowseFile;
        public event EventHandler FileSelected; 
        public string Filter { get; set; }
        public string FileName { get; private set; }

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