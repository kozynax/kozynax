using ZXMAK2.Engine;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.WinForms.Lib;

namespace Kozynax.UI
{
    public class GenericSoundSettings : DeviceSettings<ISoundRenderer>
    {
        public TrackBar Volume { get; }
        public ISoundRenderer Device { get; set; }

        public GenericSoundSettings()
        {
            Volume = new TrackBar()
            {
                Minimum = 0,
                Maximum = 100,
            };
        }

        public override void Init(BusManager bmgr, IHostService host, ISoundRenderer device)
        {
            Device = device;
            Volume.Value = device.Volume;
        }

        public override void Apply()
        {
            Device.Volume = Volume.Value;
        }
    }
}
