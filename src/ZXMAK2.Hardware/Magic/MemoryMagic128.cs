using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Hardware.Spectrum;

namespace ZXMAK2.Hardware.Magic
{
    public class MemoryMagic128 : MemorySpectrum128
    {
        public MemoryMagic128()
            : base("Magic-07")
        {
            Name = "Magic 128K";
            Description = "ZX Spectrum 128 memory for Magic-07 (7FFD write-only)";
        }

        public override void BusInit(IBusManager bmgr)
        {
            bmgr.Events.SubscribeWrIo(0x8002, 0x0000, WritePort7FFD);
            bmgr.Events.SubscribeReset(BusReset);
            // No 7FFD read: extra keyboard #xxFC aliases A15=0 / A1=0.
            MemoryBaseBusInit(bmgr);
        }

        private void WritePort7FFD(ushort addr, byte value, ref bool handled)
        {
            if ((CMR0 & 0x20) == 0)
                CMR0 = value;
        }

        private void MemoryBaseBusInit(IBusManager bmgr)
        {
            m_ula = bmgr.FindDevice<UlaDeviceBase>();
            bmgr.Events.SubscribeRdMem(0xC000, 0x0000, ReadMem0000);
            bmgr.Events.SubscribeRdMem(0xC000, 0x4000, ReadMem4000);
            bmgr.Events.SubscribeRdMem(0xC000, 0x8000, ReadMem8000);
            bmgr.Events.SubscribeRdMem(0xC000, 0xC000, ReadMemC000);
            bmgr.Events.SubscribeRdMemM1(0xC000, 0x0000, ReadMem0000);
            bmgr.Events.SubscribeRdMemM1(0xC000, 0x4000, ReadMem4000);
            bmgr.Events.SubscribeRdMemM1(0xC000, 0x8000, ReadMem8000);
            bmgr.Events.SubscribeRdMemM1(0xC000, 0xC000, ReadMemC000);
            bmgr.Events.SubscribeWrMem(0xC000, 0x0000, WriteMem0000);
            bmgr.Events.SubscribeWrMem(0xC000, 0x4000, WriteMem4000);
            bmgr.Events.SubscribeWrMem(0xC000, 0x8000, WriteMem8000);
            bmgr.Events.SubscribeWrMem(0xC000, 0xC000, WriteMemC000);
        }
    }
}
