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

        public event EventHandler Redraw;
        public event ShowWizardEventHandler ShowWizard;
        public event EventHandler Closed;

        public BusManager WorkBus { get; private set; }
        public IHostService Host { get; private set; }
        
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
            Up = new Button();
            Up.Clicked += Up_Clicked;

            Down = new Button();
            Down.Clicked += Down_Clicked;

            AddDevice = new Button();
            AddDevice.Clicked += AddDevice_Clicked;

            RemoveDevice = new Button();
            RemoveDevice.Clicked += RemoveDevice_Clicked;

            Wizard = new Button();
            Wizard.Clicked += Wizard_Clicked;

            Apply = new Button();
            Apply.Clicked += Apply_Clicked;

            Cancel = new Button();
            Cancel.Clicked += Cancel_Clicked;

            Devices = new ListView<BusDeviceBase>();
            Devices.SelectedIndexChanged += Devices_SelectedIndexChanged;
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
            Devices.List.Clear();

            foreach (var device in WorkBus.FindDevices<BusDeviceBase>())
                Devices.List.Add(device);

            Devices.SelectedIndex = Devices.List.Any() ? 0 : -1;
            Redraw(this, EventArgs.Empty);
        }

        private void RemoveDevice_Clicked(object sender, EventArgs e)
        {
            int index = Devices.SelectedIndex;
            if (index < 0 && index >= Devices.List.Count)
                return;

            var device = Devices.List[index];
            Devices.List.Remove(device);
            WorkBus.Remove(device);

            // var control = _deviceConfigurationControls[device];
            // _deviceConfigurationControls.Remove(device);

            Redraw?.Invoke(this, EventArgs.Empty);
        }

        private void Devices_SelectedIndexChanged(object sender, int index)
        {
            bool allowRemove = index >= 0 && index < Devices.List.Count;

            AddDevice.Enabled = true;
            RemoveDevice.Enabled = allowRemove;
            Up.Enabled = IsMoveUpAllowed();
            Down.Enabled = IsMoveDownAllowed();

            Redraw?.Invoke(this, EventArgs.Empty);
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

            Redraw?.Invoke(this, EventArgs.Empty);
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
