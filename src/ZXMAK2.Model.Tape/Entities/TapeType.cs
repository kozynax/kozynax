namespace ZXMAK2.Model.Tape.Entities
{
    public enum TapeType
    {
        ROM = 0x10,
        TURBO = 0x11,
        PURE_TONE = 0x12,
        PULSES = 0x13,
        PURE_DATA = 0x14,
        RAW_DATA = 0x15,
        CSW_RECORDING = 0x18,
        GENERALIZED_DATA = 0x19,
        PAUSE = 0x20,
        GROUP_START = 0x21,
        GROUP_END = 0x22,
        JUMP = 0x23,
        LOOP_START = 0x24,
        LOOP_END = 0x25,
        CALL_SEQUENCE = 0x26,
        RET_FROM_SEQUENCE = 0x27,
        SELECT = 0x28,
        STOP48 = 0x2A,
        SET_SIGNAL_LEVEL = 0x2B,
        COMMENT = 0x30,
        MESSAGE = 0x31,
        ARCHIVE_INFO = 0x32,
        HARDWARE = 0x33,
        CUSTOM = 0x35,
        CONCAT = 0x5A,
        RLE_PULSE = 0x100,
        PULSE_SEQUENCE = 0x101,
        DATA_BLOCK = 0x102,
    }
}
