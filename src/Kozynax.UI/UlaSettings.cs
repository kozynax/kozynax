using System;
using System.Collections.Generic;
using System.Linq;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Hardware;

namespace Kozynax.UI
{
    public class UlaSettings : SingleListViewDeviceSettings<UlaDeviceBase, BusDeviceDescriptor>
    {
        protected override string ListLabel => "Type:";

        protected override IEnumerable<BusDeviceDescriptor> GetListData()
            => DeviceEnumerator.SelectByType<IUlaDevice>().OrderBy(u => u.Name).ToList();

        protected override BusDeviceDescriptor FindSelectedItemInList(UlaDeviceBase ula)
            => List.List.FirstOrDefault(d => d.Type == ula.GetType());
        
        protected override UlaDeviceBase Apply(BusDeviceDescriptor bdd)
        {
            var ula = (UlaDeviceBase)Activator.CreateInstance(bdd.Type);
            var oldUla = BusManager.FindDevice<IUlaDevice>();
            if (oldUla != null && oldUla.GetType() != ula.GetType())
            {
                var busOldUla = oldUla as BusDeviceBase;
                var busNewUla = (BusDeviceBase)ula;
                if (busOldUla != null)
                {
                    BusManager.Remove(busOldUla);
                    ula.PortFE = oldUla.PortFE;
                }
                BusManager.Add(busNewUla);
            }

            return ula;
        }
    }
}