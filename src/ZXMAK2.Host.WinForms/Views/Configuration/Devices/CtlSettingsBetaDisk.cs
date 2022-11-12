using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.ComponentModel;
using System.Windows.Forms;
using Kozui.Interfaces;
using Kozynax.UI;
using Kozynax.UI.Base;
using ZXMAK2.Model.Disk;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.WinForms.Lib;
using CheckBox = ZXMAK2.Host.WinForms.Lib.CheckBox;


namespace ZXMAK2.Host.WinForms.Views.Configuration.Devices
{
    public partial class CtlSettingsBetaDisk : ConfigScreenControl<IBetaDiskDevice>, IComponentImplementation<BetaDiskSettings>
    {
        private BetaDiskSettings _beta;
        
        public CtlSettingsBetaDisk()
        {
            InitializeComponent();
        }

        public void Init(BetaDiskSettings betaDiskSettings)
        {
            _beta = betaDiskSettings;
            
            btnBrowseA.Click += (o, e) => _beta.DiskA.Disk.BrowseFile(_beta.DiskA.Disk.FileName);
            btnBrowseB.Click += (o, e) => _beta.DiskB.Disk.BrowseFile(_beta.DiskB.Disk.FileName);
            btnBrowseC.Click += (o, e) => _beta.DiskC.Disk.BrowseFile(_beta.DiskC.Disk.FileName);
            btnBrowseD.Click += (o, e) => _beta.DiskD.Disk.BrowseFile(_beta.DiskD.Disk.FileName);
            
            _beta.Redraw += Beta_Redraw;
            
            _beta.DiskA.Disk.OnBrowseFile += Disk_OnBrowseFile;
            _beta.DiskB.Disk.OnBrowseFile += Disk_OnBrowseFile;
            _beta.DiskC.Disk.OnBrowseFile += Disk_OnBrowseFile;
            _beta.DiskD.Disk.OnBrowseFile += Disk_OnBrowseFile;

            _beta.DiskA.Present.CheckedStateChanged += (o, e) => _beta.Refresh();
            _beta.DiskB.Present.CheckedStateChanged += (o, e) => _beta.Refresh();
            _beta.DiskC.Present.CheckedStateChanged += (o, e) => _beta.Refresh();
            _beta.DiskD.Present.CheckedStateChanged += (o, e) => _beta.Refresh();
            
            chkProtectA.CheckedChanged += (s, e) => _beta.DiskA.WriteProtect.Checked = (s as System.Windows.Forms.CheckBox).Checked;
            chkProtectB.CheckedChanged += (s, e) => _beta.DiskB.WriteProtect.Checked = (s as System.Windows.Forms.CheckBox).Checked;
            chkProtectC.CheckedChanged += (s, e) => _beta.DiskC.WriteProtect.Checked = (s as System.Windows.Forms.CheckBox).Checked;
            chkProtectD.CheckedChanged += (s, e) => _beta.DiskD.WriteProtect.Checked = (s as System.Windows.Forms.CheckBox).Checked;
            
            _beta.Refresh();
        }
        
        private void Beta_Redraw(object sender, EventArgs e)
        {
            chkNoDelay.Checked = _beta.NoDelay.Checked;
            chkLogIO.Checked = _beta.LogIO.Checked;
            
            var items = new[] {
                new { DiskSelector = _beta.DiskA, chkPresent = chkPresentA, chkProtect = chkProtectA, txtPath = txtPathA, btnBrowse = btnBrowseA },
                new { DiskSelector = _beta.DiskB, chkPresent = chkPresentB, chkProtect = chkProtectB, txtPath = txtPathB, btnBrowse = btnBrowseB },
                new { DiskSelector = _beta.DiskC, chkPresent = chkPresentC, chkProtect = chkProtectC, txtPath = txtPathC, btnBrowse = btnBrowseC },
                new { DiskSelector = _beta.DiskD, chkPresent = chkPresentD, chkProtect = chkProtectD, txtPath = txtPathD, btnBrowse = btnBrowseD },
            };

            foreach (var item in items)
            {
                item.chkPresent.Visible = item.chkProtect.Visible =
                    item.txtPath.Visible = item.btnBrowse.Visible = item.DiskSelector.Visible;
                item.chkProtect.Checked = item.DiskSelector.WriteProtect.Checked;
                item.chkPresent.Checked = item.DiskSelector.Present.Checked;
                item.txtPath.Text = item.DiskSelector.Disk.FileName;
                
                item.chkProtect.Enabled = item.txtPath.Enabled = item.btnBrowse.Enabled = item.DiskSelector.Present.Checked;
                item.chkProtect.Enabled &= item.DiskSelector.WriteProtect.Enabled;
            }
        }

        private void Disk_OnBrowseFile(FileSelector fileSelector, string initialFileName)
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
                loadDialog.DefaultExt = ""; //m_betaDisk.BetaDisk.FDD[drive].Serializer.GetDefaultExtension();
                loadDialog.FileName = initialFileName;
                loadDialog.ShowReadOnly = true;
                loadDialog.ReadOnlyChecked = true;
                loadDialog.CheckFileExists = true;
                if (loadDialog.ShowDialog() != System.Windows.Forms.DialogResult.OK)
                {
                    return;
                }
                
                fileSelector.SelectFile(loadDialog.FileName);
                disks[drive].WriteProtect.Checked |= loadDialog.ReadOnlyChecked;
            }
        }


        public override void Init(BusManager bmgr, IHostService host, IBetaDiskDevice device)
            => _beta.Init(bmgr, host, device);

        public override void Apply()
            => _beta.Apply();

        private void chkPresent_CheckedChanged(object sender, EventArgs e)
        {
            var map = new Dictionary<System.Windows.Forms.CheckBox, DiskSelector>() {
                { chkPresentA, _beta.DiskA },
                { chkPresentB, _beta.DiskB },
                { chkPresentC, _beta.DiskC },
                { chkPresentD, _beta.DiskD },
            };
            var checkBox = sender as System.Windows.Forms.CheckBox;

            map[checkBox].Present.Checked = checkBox.Checked;
        }
    }
}
