using System;
using System.IO;

namespace ZXMAK2.Host.WinForms.Lib
{
    public class FileSelector : KozuiControl
    {
        public const int DefaultDisplayMaxLength = 14;
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

        /// <summary>Short label for UI (basename, ellipsis when needed). Full path stays in <see cref="FileName"/>.</summary>
        public static string FormatDisplayFileName(string path, int maxLength = DefaultDisplayMaxLength)
        {
            if (string.IsNullOrEmpty(path))
                return "(empty)";
            if (maxLength < 4)
                maxLength = 4;

            var display = Path.GetFileName(path);
            if (string.IsNullOrEmpty(display))
                display = path;
            if (display.Length <= maxLength)
                return display;
            return "..." + display.Substring(display.Length - (maxLength - 3));
        }
    }
}
