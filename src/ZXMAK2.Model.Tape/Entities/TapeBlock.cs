using System.Collections.Generic;
using ZXMAK2.Model.Tape.Interfaces;

namespace ZXMAK2.Model.Tape.Entities
{
    public class TapeBlock : ITapeBlock
    {
        public List<TapePeriod> Periods = new List<TapePeriod>();
        public byte[] TapData;

        public TapeType Type { get; set; }
        public string Description { get; set; }
        public int Tag { get; set; }
        public int[] TagArray { get; set; }
        public int Pause { get; set; }

        public int Count
        {
            get { return Periods.Count; }
        }

        public bool GetEdge(int index, out int tstates, out TapeFlags flags)
        {
            if (index < 0 || index >= Periods.Count)
            {
                tstates = 0;
                flags = (TapeFlags)0;
                return false;
            }
            var period = Periods[index];
            tstates = period.Tstates;
            flags = period.Flags;
            return true;
        }

        public byte[] GetData()
        {
            return TapData;
        }
    }
}
