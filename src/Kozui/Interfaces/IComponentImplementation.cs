using System;
using Kozynax.UI;

namespace Kozui.Interfaces
{
    public interface IComponentImplementation<TSettings, TDevice> : IUiImplementation<TSettings>
        where TSettings: DeviceSettings<TDevice>
    {
        
    }
}