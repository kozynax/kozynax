using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Kozui.Interfaces;
using Kozynax.UI;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.WinForms.BindingTools;

namespace ZXMAK2.Host.WinForms.Views
{
    public partial class FormAddDeviceWizard : Form, IViewImplementation<AddDeviceDialog>
    {
        private AddDeviceDialog _addDeviceDialog;
        private KozuiBinder _binder;

        public FormAddDeviceWizard()
        {
            InitializeComponent();
            tabControl.ItemSize = new Size(0, 1);
        }

        public void Init(AddDeviceDialog addDeviceDialog)
        {
            _addDeviceDialog = addDeviceDialog;

            _binder?.Dispose();
            _binder = new KozuiBinder();
            _binder.BindEnabled(_addDeviceDialog.Finish, btnNext);
            _binder.BindText(_addDeviceDialog.DeviceDescription, txtDescription);

            _addDeviceDialog.Categories.List.ListChanged += Categories_ListChanged;
            _addDeviceDialog.Categories.PropertyChanged += Categories_PropertyChanged;
            _addDeviceDialog.Devices.List.ListChanged += Devices_ListChanged;
            _addDeviceDialog.Devices.PropertyChanged += Devices_PropertyChanged;
            _addDeviceDialog.CloseRequested += AddDeviceDialog_CloseRequested;

            _binder.Track(() =>
            {
                _addDeviceDialog.Categories.List.ListChanged -= Categories_ListChanged;
                _addDeviceDialog.Categories.PropertyChanged -= Categories_PropertyChanged;
                _addDeviceDialog.Devices.List.ListChanged -= Devices_ListChanged;
                _addDeviceDialog.Devices.PropertyChanged -= Devices_PropertyChanged;
                _addDeviceDialog.CloseRequested -= AddDeviceDialog_CloseRequested;
            });
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

        private void Categories_ListChanged(object sender, ListChangedEventArgs e)
            => SyncCategories();

        private void Categories_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == null || e.PropertyName == "SelectedIndex")
                SyncCategorySelection();
        }

        private void Devices_ListChanged(object sender, ListChangedEventArgs e)
            => SyncDevices();

        private void Devices_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == null || e.PropertyName == "SelectedIndex")
                SyncDeviceSelection();
        }

        private void SyncCategories()
        {
            lstCategory.BeginUpdate();
            lstCategory.ItemSelectionChanged -= lstCategory_ItemSelectionChanged;
            try
            {
                lstCategory.Items.Clear();
                lstCategory.SelectedIndices.Clear();

                foreach (var category in _addDeviceDialog.Categories.List)
                {
                    var lvi = new ListViewItem
                    {
                        Tag = category,
                        Text = string.Format("{0}", category),
                        ImageIndex = FormMachineSettings.FindImageIndex(category),
                    };
                    lstCategory.Items.Add(lvi);
                }

                SyncCategorySelection();
            }
            finally
            {
                lstCategory.ItemSelectionChanged += lstCategory_ItemSelectionChanged;
                lstCategory.EndUpdate();
            }
        }

        private void SyncCategorySelection()
        {
            var selectedCategoryIndex = _addDeviceDialog.Categories.SelectedIndex;
            lstCategory.ItemSelectionChanged -= lstCategory_ItemSelectionChanged;
            lstCategory.SelectedIndices.Clear();
            if (selectedCategoryIndex >= 0 && selectedCategoryIndex < lstCategory.Items.Count)
                lstCategory.SelectedIndices.Add(selectedCategoryIndex);
            lstCategory.ItemSelectionChanged += lstCategory_ItemSelectionChanged;
        }

        private void SyncDevices()
        {
            lstDevices.BeginUpdate();
            lstDevices.SelectedIndexChanged -= lstDevices_SelectedIndexChanged;
            try
            {
                lstDevices.Items.Clear();
                foreach (var device in _addDeviceDialog.Devices.List)
                    lstDevices.Items.Add(device);
                SyncDeviceSelection();
            }
            finally
            {
                lstDevices.SelectedIndexChanged += lstDevices_SelectedIndexChanged;
                lstDevices.EndUpdate();
            }
        }

        private void SyncDeviceSelection()
        {
            lstDevices.SelectedIndexChanged -= lstDevices_SelectedIndexChanged;
            lstDevices.SelectedIndex = _addDeviceDialog.Devices.SelectedIndex;
            lstDevices.SelectedIndexChanged += lstDevices_SelectedIndexChanged;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            BindCategoryList();
            tabControl_SelectedIndexChanged(this, EventArgs.Empty);
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

        internal void DisposeBinder()
            => _binder?.Dispose();
    }
}
