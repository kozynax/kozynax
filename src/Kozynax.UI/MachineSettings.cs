using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Kozui.Abstract;
using ZXMAK2;
using ZXMAK2.Dependency;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Host.WinForms.Lib.Layout;

namespace Kozynax.UI
{
    public class MachineSettings : ViewDescription<MachineSettings>
    {
        public class MachineConfiguration
        {
            public string Name { get; set; }
            public XmlNode Config { get; set; }
        }

        public delegate void ShowWizardEventHandler(object sender, IList<MachineConfiguration> machines);

        public event ShowWizardEventHandler ShowWizard;
        public event EventHandler Closed;
        /// <summary>Raised before bus Apply so hosts can flush device-settings panels.</summary>
        public event EventHandler Applying;

        public BusManager WorkBus { get; private set; }
        public IHostService Host { get; private set; }

        public Panel Root { get; }
        public Button Up { get; }
        public Button Down { get; }
        public Button AddDevice { get; }
        public Button RemoveDevice { get; }
        public Button Wizard { get; }
        public Button Apply { get; }
        public Button Cancel { get; }
        public ListView<BusDeviceBase> Devices { get; }
        public Placeholder DeviceProperties { get; }

        private IVirtualMachine m_vm;
        private readonly MachinesConfig m_machines = new MachinesConfig();
        private List<MachineConfiguration> _knownMachines;

        public MachineSettings()
        {
            Up = new Button { Text = "Up" };
            Up.Clicked += Up_Clicked;

            Down = new Button { Text = "Dn" };
            Down.Clicked += Down_Clicked;

            AddDevice = new Button { Text = "Add" };
            AddDevice.Clicked += AddDevice_Clicked;

            RemoveDevice = new Button { Text = "Remove" };
            RemoveDevice.Clicked += RemoveDevice_Clicked;

            Wizard = new Button { Text = "Wizard" };
            Wizard.Clicked += Wizard_Clicked;

            Apply = new Button { Text = "Apply" };
            Apply.Clicked += Apply_Clicked;

            Cancel = new Button { Text = "Cancel" };
            Cancel.Clicked += Cancel_Clicked;

            Devices = new ListView<BusDeviceBase>
            {
                ItemTextSelector = d => d == null ? string.Empty : $"{d.Category}: {d.Name}",
                Dock = Dock.Left,
                MinWidth = 28,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            Devices.SelectedIndexChanged += Devices_SelectedIndexChanged;

            DeviceProperties = new Placeholder
            {
                Dock = Dock.Fill,
                Margin = new Thickness(1, 0, 0, 0),
            };

            Root = BuildTree();
        }

        private Panel BuildTree()
        {
            var title = new Label
            {
                Text = "Machine Settings",
                Dock = Dock.Top,
                Margin = new Thickness(0, 0, 0, 1),
            };

            var help = new Label
            {
                Text = "Tab focus  Enter act  Up/Dn list  Esc cancel",
                Dock = Dock.Bottom,
                Margin = new Thickness(0, 1, 0, 0),
            };

            var deviceButtons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 1,
                Dock = Dock.Bottom,
                Margin = new Thickness(0, 1, 0, 0),
            };
            deviceButtons.Add(AddDevice);
            deviceButtons.Add(RemoveDevice);
            deviceButtons.Add(Up);
            deviceButtons.Add(Down);

            var actionButtons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 1,
                Dock = Dock.Bottom,
                Margin = new Thickness(0, 1, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            actionButtons.Add(Wizard);
            actionButtons.Add(Apply);
            actionButtons.Add(Cancel);

            var body = new DockPanel
            {
                Dock = Dock.Fill,
            };
            body.Add(Devices);
            body.Add(DeviceProperties);

            var root = new DockPanel { Margin = new Thickness(1) };
            root.Add(title);
            root.Add(help);
            root.Add(actionButtons);
            root.Add(deviceButtons);
            root.Add(body);
            return root;
        }

        private void AddDevice_Clicked(object sender, EventArgs e)
        {
            try
            {
                var dialogUi = new AddDeviceDialog(Devices.List.ToList());
                if (dialogUi.ShowDialog(ViewHandle) == DlgResult.OK)
                {
                    var device = dialogUi.Result;
                    AddNewDevice(device);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                Locator.Resolve<IUserMessage>().Error("Add failed!\n\n{0}", ex.Message);
            }
        }

        private void Cancel_Clicked(object sender, EventArgs e)
        {
            Closed?.Invoke(sender, e);
        }

        private void Apply_Clicked(object sender, EventArgs e)
        {
            try
            {
                Applying?.Invoke(this, EventArgs.Empty);

                if (WorkBus.FindDevice<IUlaDevice>() == null)
                {
                    Locator.Resolve<IUserMessage>()
                        .Error("Bad configuration!\n\nPease add ULA device!");
                    return;
                }
                if (WorkBus.FindDevice<IMemoryDevice>() == null)
                {
                    Locator.Resolve<IUserMessage>()
                        .Error("Bad configuration!\n\nPease add Memory device!");
                    return;
                }

                if (!WorkBus.Connect())
                {
                    Locator.Resolve<IUserMessage>()
                        .Error("Apply failed!\n\nThere is a problem in your machine configuration!\nSee logs for details");
                    WorkBus.Disconnect();
                    return;
                }

                XmlDocument xml = new XmlDocument();
                XmlNode root = xml.AppendChild(xml.CreateElement("Bus"));
                WorkBus.SaveConfigXml(root);

                bool running = m_vm.IsRunning;
                m_vm.DoStop();

                var bmgr = m_vm.Bus;

                // workaround to save border color + Reset in case when memory changed
                var ula = bmgr.FindDevice<IUlaDevice>();
                var oldMemory = bmgr.FindDevice<IMemoryDevice>();
                int portFE = ula != null ? ula.PortFE : 0x00;
                bmgr.LoadConfigXml(root);
                ula = bmgr.FindDevice<IUlaDevice>();
                ula.PortFE = (byte)portFE;
                var memory = bmgr.FindDevice<IMemoryDevice>();
                if (memory != oldMemory)
                    m_vm.DoReset();

                m_vm.SaveConfig();
                if (running)
                    m_vm.DoRun();
                GC.Collect();
                Closed?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                WorkBus.Disconnect();
                Locator.Resolve<IUserMessage>()
                    .Error("Apply failed!\n\n{0}", ex.Message);
            }
        }

        private void Wizard_Clicked(object sender, EventArgs e)
        {
            ShowWizard?.Invoke(sender, _knownMachines);
        }

        public void Init(IHostService host, IVirtualMachine vm)
        {
            Host = host;
            m_vm = vm;

            WorkBus = new BusManager();
            WorkBus.Init(null, true);

            var xml = new XmlDocument();
            var root = xml.AppendChild(xml.CreateElement("Bus"));
            try
            {
                m_vm.Bus.SaveConfigXml(root);

                WorkBus.LoadConfigXml(root);
                WorkBus.Disconnect();
                initWorkBus();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void initWorkBus()
        {
            Devices.Reset(WorkBus.FindDevices<BusDeviceBase>());
            Devices.SelectedIndex = Devices.List.Any() ? 0 : -1;
        }

        private void RemoveDevice_Clicked(object sender, EventArgs e)
        {
            int index = Devices.SelectedIndex;
            if (index < 0 || index >= Devices.List.Count)
                return;

            var device = Devices.List[index];
            Devices.List.Remove(device);
            WorkBus.Remove(device);
        }

        private void Devices_SelectedIndexChanged(object sender, int index)
        {
            bool allowRemove = index >= 0 && index < Devices.List.Count;

            AddDevice.Enabled = true;
            RemoveDevice.Enabled = allowRemove;
            Up.Enabled = IsMoveUpAllowed();
            Down.Enabled = IsMoveDownAllowed();
        }

        private bool IsMoveUpAllowed()
        {
            int index = Devices.SelectedIndex;
            if (index <= 0 || index >= Devices.List.Count - 1)
                return false;
            var device = Devices.List[index];
            if (device == null)
                return false;
            if (device is IUlaDevice)
                return false;
            if (device is IMemoryDevice)
                return false;
            index = index - 1;
            if (index <= 0 || index >= Devices.List.Count - 1)
                return false;
            device = Devices.List[index];
            if (device == null)
                return false;
            if (device is IUlaDevice)
                return false;
            if (device is IMemoryDevice)
                return false;
            return true;
        }

        private bool IsMoveDownAllowed()
        {
            int index = Devices.SelectedIndex;
            if (index <= 0 || index >= Devices.List.Count - 1)
                return false;
            var device = Devices.List[index];
            if (device == null)
                return false;
            if (device is IUlaDevice)
                return false;
            if (device is IMemoryDevice)
                return false;
            index = index + 1;
            if (index <= 0 || index >= Devices.List.Count - 1)
                return false;
            device = Devices.List[index];
            if (device == null)
                return false;
            if (device is IUlaDevice)
                return false;
            if (device is IMemoryDevice)
                return false;
            return true;
        }

        private void Up_Clicked(object sender, EventArgs e)
        {
            if (!IsMoveUpAllowed())
                return;

            int indexFrom = Devices.SelectedIndex;
            int indexTo = indexFrom - 1;
            MoveDevice(indexFrom, indexTo);
        }

        private void Down_Clicked(object sender, EventArgs e)
        {
            if (!IsMoveDownAllowed())
                return;

            int indexFrom = Devices.SelectedIndex;
            int indexTo = indexFrom + 1;
            MoveDevice(indexFrom, indexTo);
        }

        private void MoveDevice(int indexFrom, int indexTo)
        {
            var deviceFrom = Devices.List[indexFrom];
            var deviceTo = Devices.List[indexTo];

            int tmp = deviceFrom.BusOrder;
            deviceFrom.BusOrder = deviceTo.BusOrder;
            deviceTo.BusOrder = tmp;

            Devices.List.RemoveAt(indexFrom);
            Devices.List.Insert(indexTo, deviceFrom);
            Devices.SelectedIndex = indexTo;

            Up.Enabled = IsMoveUpAllowed();
            Down.Enabled = IsMoveDownAllowed();
        }

        public void Init()
        {
            LoadMachines();
            Wizard.Enabled = _knownMachines.Any();
        }

        private void LoadMachines()
        {
            _knownMachines = new List<MachineConfiguration>();
            try
            {
                m_machines.Load();
                foreach (var name in m_machines.GetNames())
                {
                    var node = m_machines.GetConfig(name);
                    var machine = new MachineConfiguration() {
                        Name = name,
                        Config = node,
                    };
                    _knownMachines.Add(machine);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public void CreateMachine(MachineConfiguration machine)
        {
            var busNode = machine.Config;

            if (busNode != null)
            {
                initConfig(busNode);
            }
            else
            {
                Locator.Resolve<IUserMessage>()
                    .Error("Invalid Configuration File!");
            }
        }

        private void initConfig(XmlNode busNode)
        {
            WorkBus.Disconnect();
            WorkBus.Clear();
            WorkBus.LoadConfigXml(busNode);
            WorkBus.Disconnect();
            initWorkBus();
        }

        public void AddNewDevice(BusDeviceBase device)
        {
            WorkBus.Add(device);

            WorkBus.Sort();
            initWorkBus();
        }
    }
}
