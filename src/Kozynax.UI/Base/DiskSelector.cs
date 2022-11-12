using System;
using System.IO;
using System.Linq;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.WinForms.Lib;

namespace Kozynax.UI.Base
{
    public class DiskSelector
    {
        public event EventHandler DiskSelected;
        public FileSelector Disk { get; }
        public CheckBox WriteProtect { get; }
        public CheckBox Present { get; }
        public bool Visible { get; set; } = true;
        public IBetaDiskDevice Device { get; set; }

        public DiskSelector()
        {
            Disk = new FileSelector();
            Disk.FileSelected += Disk_FileSelected;

            WriteProtect = new CheckBox();
            Present = new CheckBox();
        }

        private void Disk_FileSelected(object sender, EventArgs e)
        {
            var fileName = Disk.FileName;

            if (!Device.LoadManagers.First().CheckCanOpenFileName(fileName))
                return;

            var isZip = fileName != string.Empty &&
                        string.Compare(Path.GetExtension(fileName), ".ZIP", true) == 0;
            
            Disk.Enabled = Present.Checked;
            WriteProtect.Enabled = Present.Checked && !isZip;
            WriteProtect.Checked |= isZip;
            
            DiskSelected?.Invoke(this, e);
        }
    }
}