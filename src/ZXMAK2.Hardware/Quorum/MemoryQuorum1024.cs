using ZXMAK2.Engine.Interfaces;

namespace ZXMAK2.Hardware.Quorum
{
	public class MemoryQuorum1024 : MemoryQuorum256
	{
		private UlaQuorum1024Plus _ula;

		public MemoryQuorum1024() : base("Quorum1024Plus", 64)
		{
			Name = "QUORUM 1024+";
			OnCmr1Change += OnOnCmr1Change;
		}

		private void OnOnCmr1Change()
		{
			if (_ula != null)
				_ula.IsModePentagon = (CMR1 & Q_PENTAGON) != 0;
		}

		public override void BusInit(IBusManager bmgr)
		{
			base.BusInit(bmgr);
			_ula = bmgr.FindDevice<UlaQuorum1024Plus>();

			// Fearure or Quorum-1024+ only: port #00 is readable
			bmgr.Events.SubscribeRdIo(0x0099, 0x0000 & 0x0099, BusReadPort0000);
		}

		private void BusReadPort0000(ushort addr, ref byte value, ref bool handled)
		{
			value = CMR1;
			handled = true;
		}

		protected override int GetRamPage()
		{
			var ramPage = CMR0 & 7;
			ramPage |= ((CMR0 & 0xE0) >> 2);

			// Port #00 D4: 0 = 128K, 1 = 1024K
			if ((CMR1 & Q_BLK_128) == 0)
				ramPage &= 0x07;
			else
				ramPage &= 0x3F;
			return ramPage;
		}
	}
}
