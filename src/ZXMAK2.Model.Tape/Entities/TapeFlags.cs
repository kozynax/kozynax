using System;

namespace ZXMAK2.Model.Tape.Entities
{
    [Flags]
    public enum TapeFlags
    {
        BLOCK = 0x01,
        STOP = 0x02,
        STOP48 = 0x04,
        NO_EDGE = 0x08,
        LEVEL_LOW = 0x10,
        LEVEL_HIGH = 0x20,
        LENGTH_SHORT = 0x40,
        LENGTH_LONG = 0x80,
        TAPE = 0x100,
        ERROR = unchecked((int)0x80000000),
    }
}
