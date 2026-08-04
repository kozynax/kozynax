using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Kozui.Interfaces;
using Kozynax.UI;
using Kozynax.UI.Base;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.WinForms.BindingTools;
using Lib = ZXMAK2.Host.WinForms.Lib;
using WinFormsButton = System.Windows.Forms.Button;
using WinFormsCheckBox = System.Windows.Forms.CheckBox;

namespace ZXMAK2.Host.WinForms.Views.Configuration.Devices
{
    public partial class CtlSettingsBetaDisk : ConfigScreenControl, IComponentImplementation<BetaDiskSettings, IBetaDiskDevice>
    {
        private BetaDiskSettings _beta;
        private KozuiBinder _binder;

        public CtlSettingsBetaDisk()
        {
            InitializeComponent();
        }

        public void Init(BetaDiskSettings betaDiskSettings)
        {
            _beta = betaDiskSettings;

            _binder?.Dispose();
            _binder = new KozuiBinder();

            _binder.BindCheckBox(_beta.NoDelay, chkNoDelay);
            _binder.BindCheckBox(_beta.LogIO, chkLogIO);

            BindDisk(_beta.DiskA, chkPresentA, chkProtectA, txtPathA, btnBrowseA);
            BindDisk(_beta.DiskB, chkPresentB, chkProtectB, txtPathB, btnBrowseB);
            BindDisk(_beta.DiskC, chkPresentC, chkProtectC, txtPathC, btnBrowseC);
            BindDisk(_beta.DiskD, chkPresentD, chkProtectD, txtPathD, btnBrowseD);

            TrackBrowse(_beta.DiskA.Disk);
            TrackBrowse(_beta.DiskB.Disk);
            TrackBrowse(_beta.DiskC.Disk);
            TrackBrowse(_beta.DiskD.Disk);
        }

        private void TrackBrowse(Lib.FileSelector disk)
        {
            disk.OnBrowseFile += Disk_OnBrowseFile;
            _binder.Track(() => disk.OnBrowseFile -= Disk_OnBrowseFile);
        }

        private void BindDisk(
            DiskSelector disk,
            WinFormsCheckBox chkPresent,
            WinFormsCheckBox chkProtect,
            TextBox txtPath,
            WinFormsButton btnBrowse)
        {
            void syncVisible()
            {
                chkPresent.Visible = chkProtect.Visible =
                    txtPath.Visible = btnBrowse.Visible = disk.Visible;
            }

            syncVisible();
            _binder.BindOneWay(disk, nameof(DiskSelector.Visible), syncVisible);

            _binder.BindCheckBox(disk.Present, chkPresent);
            _binder.BindCheckBox(disk.WriteProtect, chkProtect);
            _binder.BindText(disk.Disk, txtPath);
            _binder.BindEnabled(disk.Disk, btnBrowse);

            EventHandler browse = (o, e) => disk.Disk.BrowseFile(disk.Disk.FileName);
            btnBrowse.Click += browse;
            _binder.Track(() => btnBrowse.Click -= browse);
        }

        private void Disk_OnBrowseFile(Lib.FileSelector fileSelector, string initialFileName)
        {
            var disks = new List<DiskSelector> { _beta.DiskA, _beta.DiskB, _beta.DiskC, _beta.DiskD };
            var fileSelectors = disks.Select(d => d.Disk).ToList();
            var drive = fileSelectors.IndexOf(fileSelector);

            using (var loadDialog = new OpenFileDialog())
            {
                loadDialog.InitialDirectory = ".";
                loadDialog.SupportMultiDottedExtensions = true;
                loadDialog.Title = "Open...";
                loadDialog.Filter = _beta.Device.LoadManagers[drive].GetOpenExtFilter();
                loadDialog.DefaultExt = "";
                loadDialog.FileName = initialFileName;
                loadDialog.ShowReadOnly = true;
                loadDialog.ReadOnlyChecked = true;
                loadDialog.CheckFileExists = true;
                if (loadDialog.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                fileSelector.SelectFile(loadDialog.FileName);
                disks[drive].WriteProtect.Checked |= loadDialog.ReadOnlyChecked;
            }
        }

        public override void Apply()
            => _beta.Apply();

        internal void DisposeBinder()
            => _binder?.Dispose();
    }
}
