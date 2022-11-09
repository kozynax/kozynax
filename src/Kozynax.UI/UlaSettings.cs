using System;
using System.Linq;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Hardware;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.WinForms.Lib;

namespace Kozynax.UI
{
    public class UlaSettings : DeviceSettings
    {
        public event EventHandler Redraw;
        
        private BusManager _bmgr;
        private IHostService _host;
        public ListView<BusDeviceDescriptor> Devices { get; }

        public UlaSettings()
        {
            Devices = new ListView<BusDeviceDescriptor>();
            var list = DeviceEnumerator.SelectByType<IUlaDevice>().OrderBy(u => u.Name).ToList();
            Devices.List.Clear();
            foreach (var device in list)
                Devices.List.Add(device);
        }

        public void Init(BusManager bmgr, IHostService host, UlaDeviceBase device)
        {
            _bmgr = bmgr;
            _host = host;

            Devices.SelectedIndex = -1;
            if (device != null)
            {
                var ourItem = Devices.List.FirstOrDefault(d => d.Type == device.GetType());
                Devices.SelectedIndex = Devices.List.IndexOf(ourItem);
            }
            
            Redraw?.Invoke(this, EventArgs.Empty);
        }

        public override void Apply()
        {
            if (Devices.SelectedIndex < 0)
                return;

            var bdd = Devices.List[Devices.SelectedIndex];

            var ula = (IUlaDevice)Activator.CreateInstance(bdd.Type);
            var oldUla = _bmgr.FindDevice<IUlaDevice>();
            if (oldUla != null && oldUla.GetType() != ula.GetType())
            {
                var busOldUla = oldUla as BusDeviceBase;
                var busNewUla = (BusDeviceBase)ula;
                if (busOldUla != null)
                {
                    _bmgr.Remove(busOldUla);
                    ula.PortFE = oldUla.PortFE;
                }
                _bmgr.Add(busNewUla);
            }
            Init(_bmgr, _host, (UlaDeviceBase)ula);
        }
    }
}