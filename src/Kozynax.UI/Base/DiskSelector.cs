using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.WinForms.Lib;

namespace Kozynax.UI.Base
{
    public class DiskSelector : INotifyPropertyChanged
    {
        public event EventHandler DiskSelected;
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _visible = true;

        public FileSelector Disk { get; }
        public CheckBox WriteProtect { get; }
        public CheckBox Present { get; }

        public bool Visible
        {
            get => _visible;
            set
            {
                if (_visible == value)
                    return;
                _visible = value;
                OnPropertyChanged();
            }
        }

        public IBetaDiskDevice Device { get; set; }

        public DiskSelector()
        {
            Disk = new FileSelector();
            Disk.FileSelected += Disk_FileSelected;

            WriteProtect = new CheckBox();
            Present = new CheckBox();
            Present.CheckedStateChanged += (o, e) => UpdateEnabledState();
        }

        private void Disk_FileSelected(object sender, EventArgs e)
        {
            var fileName = Disk.FileName;
            if (Device != null &&
                !Device.LoadManagers.First().CheckCanOpenFileName(fileName))
                return;

            UpdateEnabledState();
            DiskSelected?.Invoke(this, e);
        }

        public void UpdateEnabledState()
        {
            var fileName = Disk.FileName ?? string.Empty;
            var isZip = fileName != string.Empty &&
                        string.Compare(Path.GetExtension(fileName), ".ZIP", true) == 0;

            Disk.Enabled = Present.Checked;
            WriteProtect.Enabled = Present.Checked && !isZip;
            if (isZip)
                WriteProtect.Checked = true;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
