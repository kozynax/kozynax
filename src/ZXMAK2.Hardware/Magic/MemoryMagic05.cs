using ZXMAK2.Engine.Entities;
using ZXMAK2.Hardware.Quorum;

namespace ZXMAK2.Hardware.Magic
{
    public class MemoryMagic05 : MemoryQuorum
    {
        public MemoryMagic05()
            : base("Magic-05", 2, 8) // 8 RAM pages satisfy UlaDeviceBase
        {
            Name = "MAGIC 05 (64K)";
        }

        public override bool IsMap48 { get { return true; } }

        public override byte CMR0
        {
            get { return 0x30; }
            set { UpdateMapping(); }
        }

        public override int GetRomIndex(RomId romId)
        {
            return romId == RomId.ROM_DOS ? 1 : 0;
        }

        protected override void UpdateMapping()
        {
            // D1 of port #00 selects the upper screen, same as Quorum-64.
            var videoPage = (CMR1 & 0x02) == 0 ? 5 : 7;
            var shadow = (CMR1 & Q_F_RAM) != 0;

            m_ula.SetPageMapping(videoPage, 0, 5, 2, 7);

            if (DOSEN)
                MapRead0000 = RomPages[GetRomIndex(RomId.ROM_DOS)];
            else if (shadow)
                MapRead0000 = RamPages[0];
            else
                MapRead0000 = RomPages[GetRomIndex(RomId.ROM_SOS)];

            MapRead4000 = RamPages[5];
            MapRead8000 = RamPages[2];
            MapReadC000 = RamPages[7];

            MapWrite0000 = DOSEN ? m_trashPage : RamPages[0];
            MapWrite4000 = MapRead4000;
            MapWrite8000 = MapRead8000;
            MapWriteC000 = MapReadC000;

            Map48[0] = DOSEN ? GetRomIndex(RomId.ROM_DOS) : 0;
            Map48[1] = 5;
            Map48[2] = 2;
            Map48[3] = 7;
        }
    }
}
