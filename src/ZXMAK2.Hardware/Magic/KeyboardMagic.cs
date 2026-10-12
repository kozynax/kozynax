using ZXMAK2.Engine.Entities;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Hardware.Quorum;

namespace ZXMAK2.Hardware.Magic
{
    public class KeyboardMagic : KeyboardQuorum
    {
        public KeyboardMagic()
        {
            Category = BusDeviceCategory.Keyboard;
            Name = "KEYBOARD MAGIC";
            Description = "Magic extended keyboard";
        }
        
        public override void BusInit(IBusManager bmgr)
        {
            m_busManager = bmgr;
            bmgr.Events.SubscribeRdIo(0x99, 0x98, readPortFE);
            bmgr.Events.SubscribeRdIo(0x1F, 0x1C, readPortKbdExt);
            
            m_cpu = bmgr.CPU;
        }
    }
}