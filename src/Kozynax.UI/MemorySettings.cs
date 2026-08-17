using System;
using System.Linq;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Hardware;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Host.WinForms.Lib.Layout;

namespace Kozynax.UI
{
    /// <summary>
    /// Memory device type + ROM-set picker (WinForms <c>CtlSettingsMemory</c>).
    /// </summary>
    public class MemorySettings : DeviceSettings<IMemoryDevice>
    {
        private BusManager _bus;
        private IHostService _host;
        private bool _suppressTypeChanged;

        public IMemoryDevice Device { get; private set; }

        public Label TypeTitle { get; }
        public ListView<BusDeviceDescriptor> TypeList { get; }
        public Label RomSetTitle { get; }
        public ListView<string> RomSetList { get; }

        public MemorySettings()
        {
            TypeTitle = new Label { Text = "Type:" };
            TypeList = new ListView<BusDeviceDescriptor>
            {
                MinHeight = 4,
                ActivateOnClick = false,
                ActivateOnSecondClick = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            RomSetTitle = new Label { Text = "ROM-set:" };
            RomSetList = new ListView<string>
            {
                MinHeight = 4,
                ActivateOnClick = false,
                ActivateOnSecondClick = false,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };

            TypeList.SelectedIndexChanged += (_, __) => OnTypeSelectionChanged();
        }

        public override void Init(BusManager bmgr, IHostService host, IMemoryDevice device)
        {
            _bus = bmgr;
            _host = host;
            Device = device;

            _suppressTypeChanged = true;
            try
            {
                TypeList.Reset(
                    DeviceEnumerator.SelectByType<IMemoryDevice>().OrderBy(d => d.Name).ToList());

                TypeList.SelectedIndex = -1;
                if (device != null)
                {
                    var busDevice = device as BusDeviceBase;
                    if (busDevice != null)
                    {
                        for (var i = 0; i < TypeList.List.Count; i++)
                        {
                            if (TypeList.List[i].Type == busDevice.GetType())
                            {
                                TypeList.SelectedIndex = i;
                                break;
                            }
                        }
                    }
                }

                RomSetList.Reset(RomPack.GetRomSetNames().OrderBy(n => n).ToList());
            }
            finally
            {
                _suppressTypeChanged = false;
            }

            OnTypeSelectionChanged();
        }

        public override void Apply()
        {
            if (TypeList.SelectedIndex < 0 || _bus == null)
                return;

            var bdd = TypeList.List[TypeList.SelectedIndex];
            var memory = _bus.FindDevice<IMemoryDevice>();
            if (memory != null && memory.GetType() != bdd.Type)
            {
                _bus.Remove((BusDeviceBase)memory);
                memory = (IMemoryDevice)Activator.CreateInstance(bdd.Type);
                _bus.Add((BusDeviceBase)memory);
            }

            var memoryBase = memory as MemoryBase;
            if (memoryBase != null
                && RomSetList.Visible
                && RomSetList.SelectedIndex >= 0
                && RomSetList.SelectedIndex < RomSetList.List.Count)
            {
                memoryBase.RomSetName = RomSetList.List[RomSetList.SelectedIndex];
            }

            Init(_bus, _host, memory);
        }

        private void OnTypeSelectionChanged()
        {
            if (_suppressTypeChanged)
                return;

            var bdd = TypeList.SelectedIndex >= 0 && TypeList.SelectedIndex < TypeList.List.Count
                ? TypeList.List[TypeList.SelectedIndex]
                : null;

            if (bdd == null || !typeof(MemoryBase).IsAssignableFrom(bdd.Type))
            {
                RomSetTitle.Visible = false;
                RomSetList.Visible = false;
                return;
            }

            RomSetTitle.Visible = true;
            RomSetList.Visible = true;

            MemoryBase memory = null;
            if (Device != null && bdd.Type == Device.GetType())
                memory = Device as MemoryBase;
            if (memory == null)
                memory = (MemoryBase)Activator.CreateInstance(bdd.Type);

            RomSetList.SelectedIndex = -1;
            if (memory == null || string.IsNullOrEmpty(memory.RomSetName))
                return;

            for (var i = 0; i < RomSetList.List.Count; i++)
            {
                if (string.Compare(memory.RomSetName, RomSetList.List[i], StringComparison.OrdinalIgnoreCase) == 0)
                {
                    RomSetList.SelectedIndex = i;
                    break;
                }
            }
        }
    }
}
