using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Kozui.Interfaces;
using Kozynax.UI;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Host.Presentation.Interfaces;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.WinForms.BindingTools;
using ZXMAK2.Host.WinForms.Views.Configuration.Devices;
using ZXMAK2.Host.WinForms.Tools;
using ZXMAK2.Resources;


namespace ZXMAK2.Host.WinForms.Views
{
    public class FormMachineSettings : Form, IMachineSettingsView
    {
        #region Windows Form Designer generated code

        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.ListView lstNavigation;
        private System.Windows.Forms.ColumnHeader colDevice;
        private System.Windows.Forms.ColumnHeader colSummary;
        private System.Windows.Forms.Panel pnlSettings;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnApply;
        private Button btnUp;
        private Button btnDown;
        private Button btnWizard;
        private ContextMenuStrip ctxMenuWizard;
        private ZXMAK2.Host.WinForms.Controls.Separator separator1;
        private System.Windows.Forms.ImageList imageList;

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.ListViewItem listViewItem1 = new System.Windows.Forms.ListViewItem("Memory", 0);
            System.Windows.Forms.ListViewItem listViewItem2 = new System.Windows.Forms.ListViewItem("ULA", 1);
            System.Windows.Forms.ListViewItem listViewItem3 = new System.Windows.Forms.ListViewItem("Processor", 2);
            System.Windows.Forms.ListViewItem listViewItem4 = new System.Windows.Forms.ListViewItem("Beta Disk Interface", 3);
            System.Windows.Forms.ListViewItem listViewItem5 = new System.Windows.Forms.ListViewItem("Beeper", 4);
            System.Windows.Forms.ListViewItem listViewItem6 = new System.Windows.Forms.ListViewItem("Sound", 5);
            System.Windows.Forms.ListViewItem listViewItem7 = new System.Windows.Forms.ListViewItem("Tape", 6);
            System.Windows.Forms.ListViewItem listViewItem8 = new System.Windows.Forms.ListViewItem("Display", 7);
            this.lstNavigation = new System.Windows.Forms.ListView();
            this.colDevice = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.colSummary = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.imageList = new System.Windows.Forms.ImageList(this.components);
            this.pnlSettings = new System.Windows.Forms.Panel();
            this.btnRemove = new System.Windows.Forms.Button();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnApply = new System.Windows.Forms.Button();
            this.btnUp = new System.Windows.Forms.Button();
            this.btnDown = new System.Windows.Forms.Button();
            this.btnWizard = new System.Windows.Forms.Button();
            this.ctxMenuWizard = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.separator1 = new ZXMAK2.Host.WinForms.Controls.Separator();
            this.SuspendLayout();
            // 
            // lstNavigation
            // 
            this.lstNavigation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)));
            this.lstNavigation.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.colDevice,
            this.colSummary});
            this.lstNavigation.FullRowSelect = true;
            this.lstNavigation.HideSelection = false;
            this.lstNavigation.Items.AddRange(new System.Windows.Forms.ListViewItem[] {
            listViewItem1,
            listViewItem2,
            listViewItem3,
            listViewItem4,
            listViewItem5,
            listViewItem6,
            listViewItem7,
            listViewItem8});
            this.lstNavigation.Location = new System.Drawing.Point(12, 12);
            this.lstNavigation.MultiSelect = false;
            this.lstNavigation.Name = "lstNavigation";
            this.lstNavigation.Size = new System.Drawing.Size(260, 332);
            this.lstNavigation.SmallImageList = this.imageList;
            this.lstNavigation.TabIndex = 0;
            this.lstNavigation.UseCompatibleStateImageBehavior = false;
            this.lstNavigation.View = System.Windows.Forms.View.Details;
            this.lstNavigation.ItemSelectionChanged += new System.Windows.Forms.ListViewItemSelectionChangedEventHandler(this.lstNavigation_ItemSelectionChanged);
            // 
            // colDevice
            // 
            this.colDevice.Text = "Device";
            this.colDevice.Width = 128;
            // 
            // colSummary
            // 
            this.colSummary.Text = "Summary";
            this.colSummary.Width = 128;
            // 
            // imageList
            // 
            this.imageList.TransparentColor = System.Drawing.Color.Transparent;
            this.imageList.Images.Add(ResourceImages.RAM);
            this.imageList.Images.Add(ResourceImages.PCB);
            this.imageList.Images.Add(ResourceImages.ULA);
            this.imageList.Images.Add(ResourceImages.FDD);
            this.imageList.Images.Add(ResourceImages.BEEPER);
            this.imageList.Images.Add(ResourceImages.AY8910);
            this.imageList.Images.Add(ResourceImages.TAPE);
            this.imageList.Images.Add(ResourceImages.KBD);
            this.imageList.Images.Add(ResourceImages.MOUS);
            this.imageList.Images.Add(ResourceImages.DISPLAY);
            this.imageList.Images.Add(ResourceImages.DEBUG);
            // 
            // pnlSettings
            // 
            this.pnlSettings.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlSettings.Location = new System.Drawing.Point(278, 12);
            this.pnlSettings.Name = "pnlSettings";
            this.pnlSettings.Size = new System.Drawing.Size(284, 332);
            this.pnlSettings.TabIndex = 1;
            // 
            // btnRemove
            // 
            this.btnRemove.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnRemove.Enabled = false;
            this.btnRemove.Location = new System.Drawing.Point(197, 362);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(75, 27);
            this.btnRemove.TabIndex = 2;
            this.btnRemove.Text = "Remove";
            this.btnRemove.UseVisualStyleBackColor = true;
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);
            // 
            // btnAdd
            // 
            this.btnAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAdd.Enabled = false;
            this.btnAdd.Location = new System.Drawing.Point(116, 362);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 27);
            this.btnAdd.TabIndex = 3;
            this.btnAdd.Text = "Add...";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(487, 362);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 27);
            this.btnCancel.TabIndex = 4;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // btnApply
            // 
            this.btnApply.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnApply.Location = new System.Drawing.Point(406, 362);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(75, 27);
            this.btnApply.TabIndex = 5;
            this.btnApply.Text = "Apply";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // btnUp
            // 
            this.btnUp.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnUp.Location = new System.Drawing.Point(12, 362);
            this.btnUp.Name = "btnUp";
            this.btnUp.Size = new System.Drawing.Size(27, 27);
            this.btnUp.TabIndex = 6;
            this.btnUp.Text = "/\\";
            this.btnUp.UseVisualStyleBackColor = true;
            this.btnUp.Click += new System.EventHandler(this.btnUp_Click);
            // 
            // btnDown
            // 
            this.btnDown.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDown.Location = new System.Drawing.Point(45, 362);
            this.btnDown.Name = "btnDown";
            this.btnDown.Size = new System.Drawing.Size(27, 27);
            this.btnDown.TabIndex = 7;
            this.btnDown.Text = "\\/";
            this.btnDown.UseVisualStyleBackColor = true;
            this.btnDown.Click += new System.EventHandler(this.btnDown_Click);
            // 
            // btnWizard
            // 
            this.btnWizard.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnWizard.Image = ResourceImages.Wizard;
            this.btnWizard.Location = new System.Drawing.Point(325, 362);
            this.btnWizard.Name = "btnWizard";
            this.btnWizard.Size = new System.Drawing.Size(75, 27);
            this.btnWizard.TabIndex = 8;
            this.btnWizard.Text = "Wizard";
            this.btnWizard.TextImageRelation = System.Windows.Forms.TextImageRelation.ImageBeforeText;
            this.btnWizard.UseVisualStyleBackColor = true;
            this.btnWizard.Click += new System.EventHandler(this.btnWizard_Click);
            // 
            // ctxMenuWizard
            // 
            this.ctxMenuWizard.Name = "ctxMenuWizard";
            this.ctxMenuWizard.Size = new System.Drawing.Size(61, 4);
            // 
            // separator1
            // 
            this.separator1.Alignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.separator1.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.separator1.Location = new System.Drawing.Point(-3, 350);
            this.separator1.Name = "separator1";
            this.separator1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            this.separator1.Size = new System.Drawing.Size(580, 6);
            this.separator1.TabIndex = 9;
            this.separator1.Text = "separator1";
            // 
            // FormMachineSettings
            // 
            this.AcceptButton = this.btnApply;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(574, 397);
            this.Controls.Add(this.separator1);
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.btnWizard);
            this.Controls.Add(this.btnUp);
            this.Controls.Add(this.btnDown);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.pnlSettings);
            this.Controls.Add(this.lstNavigation);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormMachineSettings";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Machine Settings";
            this.ResumeLayout(false);

        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_machineSettings != null)
                {
                    _machineSettings.Devices.List.ListChanged -= Devices_ListChanged;
                    _machineSettings.Devices.PropertyChanged -= Devices_PropertyChanged;
                    _machineSettings.ShowWizard -= machineSettings_ShowWizard;
                    _machineSettings.Closed -= _machineSettings_Closed;
                }
                _binder?.Dispose();
                _binder = null;
                if (components != null)
                    components.Dispose();
            }
            base.Dispose(disposing);
        }

        #endregion

        #region private

        private Dictionary<BusDeviceBase, ConfigScreenControl> _deviceConfigurationControls = new Dictionary<BusDeviceBase, ConfigScreenControl>();
        private MachineSettings _machineSettings;
        private KozuiBinder _binder;

        #endregion


        public FormMachineSettings()
        {
            InitializeComponent();
            lstNavigation.Items.Clear();
        }

        public void Init(MachineSettings machineSettings)
        {
            _machineSettings = machineSettings;
            _machineSettings.Init();

            _binder?.Dispose();
            _binder = new KozuiBinder();
            _binder.BindButton(_machineSettings.AddDevice, btnAdd);
            _binder.BindButton(_machineSettings.RemoveDevice, btnRemove);
            _binder.BindButton(_machineSettings.Up, btnUp);
            _binder.BindButton(_machineSettings.Down, btnDown);
            _binder.BindButton(_machineSettings.Apply, btnApply);
            _binder.BindButton(_machineSettings.Cancel, btnCancel);
            _binder.BindButton(_machineSettings.Wizard, btnWizard);

            _machineSettings.Devices.List.ListChanged += Devices_ListChanged;
            _machineSettings.Devices.PropertyChanged += Devices_PropertyChanged;
            _machineSettings.ShowWizard += machineSettings_ShowWizard;
            _machineSettings.Closed += _machineSettings_Closed;
            _machineSettings.Applying += machineSettings_Applying;

            SyncDeviceList();
            SyncSelectedDevice();
        }

        private void machineSettings_Applying(object sender, EventArgs e)
        {
            foreach (var csc in _deviceConfigurationControls)
                csc.Value.Apply();
        }

        private void _machineSettings_Closed(object sender, EventArgs e)
            => Close();

        private void machineSettings_ShowWizard(object sender, IList<MachineSettings.MachineConfiguration> machines)
        {
            ctxMenuWizard.Items.Clear();

            foreach (var m in machines)
            {
                var item = ctxMenuWizard.Items.Add(m.Name);
                item.Tag = m;
                item.Click += new EventHandler(ctxMenuWizardItem_Click);
            }

            if (ctxMenuWizard.Items.Count < 1)
                return;

            var p = new Point(btnWizard.Width, 0);

            // fix self collapse on first appearance
            if (!ctxMenuWizard.Created)
            {
                // we needs to show/hide it
                // temporary set zero height to avoid flicks
                var height = ctxMenuWizard.Height;
                ctxMenuWizard.Height = 0;
                ctxMenuWizard.Show(btnWizard, p);
                ctxMenuWizard.Hide();
                ctxMenuWizard.Height = height;
            }
            ctxMenuWizard.Show(btnWizard, p);
        }

        private void Devices_ListChanged(object sender, ListChangedEventArgs e)
            => SyncDeviceList();

        private void Devices_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == null ||
                e.PropertyName == "SelectedIndex")
                SyncSelectedDevice();
        }

        private void SyncDeviceList()
        {
            var devices = _machineSettings.Devices.List;
            int index = 0;

            lstNavigation.BeginUpdate();
            lstNavigation.ItemSelectionChanged -= lstNavigation_ItemSelectionChanged;

            while (true)
            {
                if (devices.Count <= index)
                {
                    for (int i = lstNavigation.Items.Count - 1; i >= index; i--)
                        lstNavigation.Items.RemoveAt(i);
                    break;
                }

                var device = devices[index];

                if (lstNavigation.Items.Count <= index)
                {
                    InsertListViewItem(index, device);
                }
                else if (lstNavigation.Items[index].Tag != device)
                {
                    var listItemForDevice = lstNavigation.Items.OfType<ListViewItem>().FirstOrDefault(i => i.Tag == device);
                    if (listItemForDevice != null)
                    {
                        lstNavigation.Items.Remove(listItemForDevice);
                        lstNavigation.Items.Insert(index, listItemForDevice);
                    }
                    else
                        InsertListViewItem(index, device);
                }

                index++;
            }

            lstNavigation.ItemSelectionChanged += lstNavigation_ItemSelectionChanged;
            lstNavigation.EndUpdate();

            foreach (var device in devices)
            {
                if (!_deviceConfigurationControls.ContainsKey(device))
                {
                    var control = ResolveScreenControl(_machineSettings.WorkBus, _machineSettings.Host, device);
                    _deviceConfigurationControls[device] = control;
                    pnlSettings.Controls.Add(control);
                }
            }

            foreach (var device in _deviceConfigurationControls.ToList())
            {
                if (!devices.Contains(device.Key))
                {
                    pnlSettings.Controls.Remove(device.Value);
                    _deviceConfigurationControls.Remove(device.Key);
                    device.Value.Dispose();
                }
            }

            SyncSelectedDevice();
        }

        private void SyncSelectedDevice()
        {
            foreach (var ctl in _deviceConfigurationControls.Values)
                ctl.Visible = false;

            var devices = _machineSettings.Devices.List;
            var index = _machineSettings.Devices.SelectedIndex;
            if (index < 0 || index >= devices.Count)
                return;

            var device = devices[index];
            if (_deviceConfigurationControls.TryGetValue(device, out var control))
                control.Visible = true;

            if (index < lstNavigation.Items.Count)
            {
                lstNavigation.ItemSelectionChanged -= lstNavigation_ItemSelectionChanged;
                lstNavigation.Items[index].Selected = true;
                lstNavigation.ItemSelectionChanged += lstNavigation_ItemSelectionChanged;
            }
        }

        private void InsertListViewItem(int index, BusDeviceBase device)
        {
            var lvi = new ListViewItem();
            lvi.Tag = device;
            lvi.Text = device.Category.ToString();
            lvi.SubItems.Add(device.Name);
            lvi.ImageIndex = FindImageIndex(device.Category);
            lstNavigation.Items.Insert(index, lvi);
        }

        public static int FindImageIndex(BusDeviceCategory category)
        {
            switch (category)
            {
                case BusDeviceCategory.Memory:
                    return 0;
                case BusDeviceCategory.Other:
                    return 1;
                case BusDeviceCategory.ULA:
                    return 2;
                case BusDeviceCategory.Disk:
                    return 3;
                case BusDeviceCategory.Sound:
                    return 4;
                case BusDeviceCategory.Music:
                    return 5;
                case BusDeviceCategory.Tape:
                    return 6;
                case BusDeviceCategory.Keyboard:
                    return 7;
                case BusDeviceCategory.Mouse:
                    return 8;
                case BusDeviceCategory.Debugger:
                    return 10;

                default:
                    return 1;
            }
        }

        private UserControl CreateConfigScreenControl(BusManager bmgr, IHostService host, BusDeviceBase objTarget)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var refName = typeof(ConfigScreenControl).Assembly.GetName().FullName;
                    var hasRef = asm.GetName().FullName == refName ||
                        asm.GetReferencedAssemblies()
                            .Any(name => name.FullName == refName);
                    if (!hasRef)
                    {
                        // skip assemblies without reference on assembly which contains ConfigScreenControl 
                        continue;
                    }
                    
                    var deviceType = FindGenericType(typeof(DeviceSettings<>), objTarget.GetType(), asm);
                    var componentType = FindGenericType(typeof(IComponentImplementation<,>), deviceType, asm);

                    var mi = deviceType.GetMethod("Init", new Type[] { typeof(BusManager), typeof(IHostService), objTarget.GetType() });
                    if (mi == null)
                        continue;
                    var deviceSettings = Activator.CreateInstance(deviceType);
                    mi.Invoke(deviceSettings, new object[] { bmgr, host, objTarget });

                    var component = (ConfigScreenControl)Activator.CreateInstance(componentType);
                    mi = componentType.GetMethod("Init", new Type[] { deviceType });
                    if (mi == null)
                        continue;
                    mi.Invoke(component, new[] { deviceSettings });

                    return component;
                    /* foreach (Type type in asm.GetTypes())
                     {
                         try
                         {
                             if (type.IsClass &&
                                 !type.IsAbstract &&
                                 type != typeof(CtlSettingsGenericDevice) &&
                                 typeof(ConfigScreenControl).IsAssignableFrom(type) &&
                                 typeof(UserControl).IsAssignableFrom(type))
                             {
                                 var mi = type.GetMethod("Init", new Type[] { typeof(BusManager), typeof(IHostService), objTarget.GetType() });
                                 if (mi == null)
                                     continue;
                                 var obj = (UserControl)Activator.CreateInstance(type);
                                 mi.Invoke(obj, new object[] { bmgr, host, objTarget });
                                 return obj;
                             }
                         }
                         catch (Exception ex)
                         {
                             Logger.Error(ex, type.FullName);
                         }
                     }*/
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, asm.FullName);
                    return null;
                }
            }
            return null;
        }

        private static IEnumerable<Type> IterateAllParentTypes(Type type)
        {
            while (true)
            {
                yield return type;
                
                // Check interfaces
                var interfaces = type.GetInterfaces();
                foreach (var iface in interfaces)
                    yield return iface;
                
                type = type.BaseType;
                if (type == typeof(object))
                    break;
            }
        }

        private static Type FindGenericType(Type target, Type argumentType, Assembly assembly)
        {
            var checkTypes = IterateAllParentTypes(argumentType).ToList();
            var types = GetAllImplementationsOfType(target, assembly).ToList();

            var type = types.FirstOrDefault(t => new[] { t.BaseType }.Union(t.GetInterfaces()).Any(i => i.GetGenericArguments().Any(checkTypes.Contains)));
            return type;
        }

        private static IEnumerable<Type> GetAllImplementationsOfType(Type target, Assembly assembly)
        {
            var assemblyTypes = assembly.GetTypes();
            
            Func<Type, IEnumerable<Type>, bool> getMatchingTypes = (a, s) => a?.IsGenericType ?? false
                ? s.Any(t => a.GetGenericTypeDefinition() == t)
                : s.Any(t => a == t);

            Func<IEnumerable<Type>, IEnumerable<Type>> getSubtypes = s => assemblyTypes
                .Where(a => getMatchingTypes(a.BaseType, s))
                .Union(assemblyTypes.Where(a => a.GetInterfaces().Any(i => getMatchingTypes(i, s))));
            
            var result = new HashSet<Type>(getSubtypes(new[] { target }));
            var subTypes = new HashSet<Type>(result);
            while (true)
            {
                subTypes = new HashSet<Type>(
                    assemblyTypes
                        .Where(a => getMatchingTypes(a.BaseType, subTypes))
                        .Union(assemblyTypes.Where(a => a.GetInterfaces().Any(i => getMatchingTypes(i, subTypes)))));
                
                if (subTypes.Count == 0)
                    break;

                result.UnionWith(subTypes);
            }

            return result;
        }

        public void Init(IHostService host, IVirtualMachine vm)
            => _machineSettings.Init(host, vm);

        public DlgResult ShowDialog(object owner)
        {
            var win32owner = owner as IWin32Window;
            if (win32owner != null)
            {
                return EnumMapper.GetDlgResult(base.ShowDialog(win32owner));
            }
            else
            {
                return EnumMapper.GetDlgResult(base.ShowDialog());
            }
        }

        private ConfigScreenControl ResolveScreenControl(BusManager workBus, IHostService host, BusDeviceBase device)
        {
            var control = (ConfigScreenControl)CreateConfigScreenControl(workBus, host, device);
            try
            {
                if (control != null)
                {
                    return control;
                }
                return (ConfigScreenControl)CreateGenericScreenControl(workBus, host, device);
            }
            catch
            {
                if (control != null)
                {
                    control.Dispose();
                }
                throw;
            }
        }

        private static UserControl CreateGenericScreenControl(BusManager workBus, IHostService host, BusDeviceBase device)
        {
            var control = new CtlSettingsGenericDevice();
            try
            {
                control.Init(workBus, host, device);
                return control;
            }
            catch
            {
                control.Dispose();
                throw;
            }
        }

        private void lstNavigation_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
        {
            _machineSettings.Devices.SelectedIndex = e.IsSelected ? e.ItemIndex : -1;
        }

        private void btnApply_Click(object sender, EventArgs e)
            => _machineSettings.Apply.Click(sender, e);

        private void btnRemove_Click(object sender, EventArgs e)
            => _machineSettings.RemoveDevice.Click(sender, e);

        private void btnAdd_Click(object sender, EventArgs e)
            => _machineSettings.AddDevice.Click(sender, e);

        private void btnUp_Click(object sender, EventArgs e)
           => _machineSettings.Up.Click(sender, e);

        private void btnDown_Click(object sender, EventArgs e)
           => _machineSettings.Down.Click(sender, e);

        private void btnWizard_Click(object sender, EventArgs e)
            => _machineSettings.Wizard.Click(sender, e);

        private void ctxMenuWizardItem_Click(object sender, EventArgs e)
            => _machineSettings.CreateMachine((MachineSettings.MachineConfiguration)(sender as ToolStripItem).Tag);
    }
}
