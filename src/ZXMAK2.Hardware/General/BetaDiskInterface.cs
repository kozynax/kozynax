using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Mvvm;


namespace ZXMAK2.Hardware.General
{
    public class BetaDiskInterface : FddController
    {
        private IBusManager _bmgr;
        private ICommand CommandMagic { get; set; }

        public BetaDiskInterface()
        {
            Name = "BDI-128";
            Description = "Beta Disk Interface\r\nDOSEN enabler + WD1793";
        }
        

        #region IBusDevice

        public override void BusInit(IBusManager bmgr)
        {
            _bmgr = bmgr;
            CommandMagic = new CommandDelegate(CommandMagic_OnExecute, () => true, "MAGIC");
            bmgr.AddCommandUi(CommandMagic);
            base.BusInit(bmgr);

            bmgr.Events.SubscribeRdMemM1(0xFF00, 0x3D00, BusReadMem3D00_M1);
            bmgr.Events.SubscribeRdMemM1(0xC000, 0x4000, BusReadMemRam);
            bmgr.Events.SubscribeRdMemM1(0xC000, 0x8000, BusReadMemRam);
            bmgr.Events.SubscribeRdMemM1(0xC000, 0xC000, BusReadMemRam);

            bmgr.Events.SubscribeReset(BusReset);
        }

        #endregion


        #region Private

        protected virtual void BusReadMem3D00_M1(ushort addr, ref byte value)
        {
            if (m_memory.IsRom48)
            {
                m_memory.DOSEN = true;
            }
        }

        protected virtual void BusReadMemRam(ushort addr, ref byte value)
        {
            if (m_memory.DOSEN)
            {
                m_memory.DOSEN = false;
            }
        }

        protected virtual void BusReset()
        {
            m_memory.DOSEN = false;
        }

        private void CommandMagic_OnExecute()
        {
            _bmgr.Events.RequestNmi(50000, () =>
            {
                m_memory.DOSEN = true;
            }, () => m_memory.IsRom48);
        }

        #endregion
    }
}
