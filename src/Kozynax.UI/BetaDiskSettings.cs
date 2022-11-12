using System;
using System.IO;
using Kozynax.UI.Base;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Model.Disk;

namespace Kozynax.UI
{
    public class BetaDiskSettings : DeviceSettings<IBetaDiskDevice>
    {
        public event EventHandler Redraw;
        
        private BusManager _bmgr;
        public IBetaDiskDevice Device { get; private set; }
        public CheckBox NoDelay {get;}
        public CheckBox LogIO {get;}

        public DiskSelector DiskA { get; }
        public DiskSelector DiskB { get; }
        public DiskSelector DiskC { get; }
        public DiskSelector DiskD { get; }

        public BetaDiskSettings()
        {
            NoDelay = new CheckBox();
            LogIO = new CheckBox();

            DiskA = new DiskSelector();
            DiskB = new DiskSelector();
            DiskC = new DiskSelector();
            DiskD = new DiskSelector();
            DiskA.DiskSelected += (o, e) => Refresh();
            DiskB.DiskSelected += (o, e) => Refresh();
            DiskC.DiskSelected += (o, e) => Refresh();
            DiskD.DiskSelected += (o, e) => Refresh();
        }
        
        public override void Init(BusManager bmgr, IHostService host, IBetaDiskDevice device)
        {
            _bmgr = bmgr;
            Device = device;
            NoDelay.Checked = device.NoDelay;
            LogIO.Checked = Device.LogIo;
            
            initDrive(GetImage(0), DiskA);
            initDrive(GetImage(1), DiskB);
            initDrive(GetImage(2), DiskC);
            initDrive(GetImage(3), DiskD);
            
            Refresh();
        }

        public void Refresh() => Redraw?.Invoke(this, EventArgs.Empty);

        private DiskImage GetImage(int index)
        {
            return Device.FDD.Length > index ? Device.FDD[index] : null;
        }
        
        private void initDrive(
            DiskImage diskImage, 
            DiskSelector diskSelector)
        {
            diskSelector.Device = Device;
            if (diskImage != null)
            {
                diskSelector.Visible = true;
                diskSelector.Present.Checked = diskImage.Present;
                diskSelector.Disk.SelectFile(diskImage.FileName);
                diskSelector.WriteProtect.Checked |= diskImage.IsWP;
            }
            else
                diskSelector.Visible = false;
        }

        private void applyDrive(DiskImage diskImage, DiskSelector diskSelector)
        {
            if (diskImage == null)
            {
                return;
            }
            var fileName = diskSelector.Disk.FileName;
            if (fileName != string.Empty)
            {
                if (!File.Exists(Path.GetFullPath(fileName)) && diskSelector.Present.Checked)
                    throw new FileNotFoundException($"File not found: {fileName}");
                fileName = Path.GetFullPath(fileName);
            }
            diskImage.Present = diskSelector.Present.Checked;
            diskImage.FileName = fileName;
            diskImage.IsWP = diskSelector.WriteProtect.Checked;
        }

        public override void Apply()
        {
            Device.NoDelay = NoDelay.Checked;
            Device.LogIo = LogIO.Checked;
            applyDrive(GetImage(0), DiskA);
            applyDrive(GetImage(1), DiskB);
            applyDrive(GetImage(2), DiskC);
            applyDrive(GetImage(3), DiskD);
        }
    }
}