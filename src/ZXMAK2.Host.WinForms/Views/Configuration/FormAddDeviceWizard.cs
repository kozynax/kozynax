using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Kozui.Interfaces;
using Kozynax.UI;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Host.Entities;

namespace ZXMAK2.Host.WinForms.Views
{
    public partial class FormAddDeviceWizard : Form, IViewImplementation<AddDeviceDialog>
    {
        private AddDeviceDialog _addDeviceDialog;
        public FormAddDeviceWizard()
        {
            InitializeComponent();
            tabControl.ItemSize = new Size(0, 1);
        }

        public void Init(AddDeviceDialog addDeviceDialog)
        {
            _addDeviceDialog = addDeviceDialog;
            _addDeviceDialog.Redraw += AddDeviceDialog_Redraw;
            _addDeviceDialog.CloseRequested += AddDeviceDialog_CloseRequested;
        }

        DlgResult IViewImplementation<AddDeviceDialog>.ShowDialog(object owner)
        {
            if (ShowDialog((IWin32Window)owner) == DialogResult.OK)
                return DlgResult.OK;
            return DlgResult.Cancel;
        }

        private void AddDeviceDialog_CloseRequested(object sender, EventArgs e)
        {
            DialogResult = _addDeviceDialog.Result != null ? DialogResult.OK : DialogResult.Cancel;
            Close();
        }

        private void AddDeviceDialog_Redraw(object sender, EventArgs e)
        {
            if (!Enumerable.SequenceEqual(
                    lstCategory.Items.Cast<ListViewItem>().Where(i => i.Tag != null).Select(i => (BusDeviceCategory)i.Tag),
                    _addDeviceDialog.Categories.List))
            {
                lstCategory.Items.Clear();
                lstCategory.SelectedIndices.Clear();
                
                var categories = _addDeviceDialog.Categories.List.ToList();
                foreach (var category in categories)
                {
                    ListViewItem lvi = new ListViewItem();
                    lvi.Tag = category;
                    lvi.Text = string.Format("{0}", category);
                    lvi.ImageIndex = FormMachineSettings.FindImageIndex(category);
                    lstCategory.Items.Add(lvi);
                }

                lstCategory.ItemSelectionChanged -= lstCategory_ItemSelectionChanged;
                var selectedCategoryIndex = _addDeviceDialog.Categories.SelectedIndex;
                if (selectedCategoryIndex > 0)
                    lstCategory.SelectedIndices.Add(selectedCategoryIndex);
                lstCategory.ItemSelectionChanged += lstCategory_ItemSelectionChanged;
            }

            if (!Enumerable.SequenceEqual(
                    lstDevices.Items.Cast<BusDeviceDescriptor>(),
                    _addDeviceDialog.Devices.List.AsEnumerable()))
            {
                lstDevices.Items.Clear();
                var devices = _addDeviceDialog.Devices.List.ToList();
                foreach (var device in devices)
                    lstDevices.Items.Add(device);

                lstDevices.SelectedIndexChanged -= lstDevices_SelectedIndexChanged;
                lstDevices.SelectedIndex = _addDeviceDialog.Devices.SelectedIndex;
                lstDevices.SelectedIndexChanged += lstDevices_SelectedIndexChanged;
            }
            
            var lines = (_addDeviceDialog.DeviceDescription.Text ?? string.Empty).Split(
                new string[] { Environment.NewLine, "\r", "\n" },
                StringSplitOptions.None);
            txtDescription.Lines = lines;
            btnNext.Enabled = _addDeviceDialog.Finish.Enabled;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            BindCategoryList();
            tabControl_SelectedIndexChanged(this, EventArgs.Empty);
            AddDeviceDialog_Redraw(this, e);
        }

        public List<BusDeviceBase> IgnoreList { get; set; }

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (tabControl.SelectedIndex == 0)
            {
                btnBack.Enabled = false;
                btnNext.Text = "Finish";
            }
        }

        private void btnNext_Click(object sender, EventArgs e)
        {
            if (tabControl.SelectedIndex == 0)
                _addDeviceDialog.Finish.Click(sender, e);
        }

        private void lstCategory_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
            => _addDeviceDialog.Categories.SelectedIndex = e.ItemIndex;

        private void lstDevices_SelectedIndexChanged(object sender, EventArgs e)
            => _addDeviceDialog.Devices.SelectedIndex = lstDevices.SelectedIndex;

        private void BindCategoryList() => _addDeviceDialog.BindCategories();

        private void lstDevices_DoubleClick(object sender, EventArgs e)
        {
            if (lstDevices.SelectedItem != null)
                btnNext_Click(lstDevices, EventArgs.Empty);
        }
    }
}
