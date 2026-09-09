using System;
using ZXMAK2.Engine;
using ZXMAK2.Hardware;
using ZXMAK2.Host.Interfaces;

namespace Kozynax.UI
{
    public abstract class DeviceSettings<T>
    {
        public abstract void Init(BusManager bmgr, IHostService host, T device);
        public abstract void Apply();
    }
}