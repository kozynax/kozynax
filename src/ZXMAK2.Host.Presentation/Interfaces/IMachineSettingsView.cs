using System;
using Kozui.Interfaces;
using Kozynax.UI;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.Entities;

namespace ZXMAK2.Host.Presentation.Interfaces
{
    public interface IMachineSettingsView : IViewImplementation<MachineSettings>
    {
        void Init(IHostService host, IVirtualMachine vm);
    }
}
