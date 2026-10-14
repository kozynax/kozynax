namespace ZXMAK2.Model.Tape.Entities
{
    public struct TapePeriod
    {
        public readonly int Tstates;
        public readonly TapeFlags Flags;

        public TapePeriod(int tstates)
            : this(tstates, (TapeFlags)0)
        {
        }

        public TapePeriod(int tstates, TapeFlags flags)
        {
            Tstates = tstates;
            Flags = flags;
        }
    }
}
