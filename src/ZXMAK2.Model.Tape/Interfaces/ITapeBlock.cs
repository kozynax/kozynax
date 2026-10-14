using ZXMAK2.Model.Tape.Entities;

namespace ZXMAK2.Model.Tape.Interfaces
{
    public interface ITapeBlock
    {
        TapeType Type { get; }
        string Description { get; }
        int Tag { get; }
        int[] TagArray { get; }
        int Pause { get; }
        int Count { get; }
        bool GetEdge(int index, out int tstates, out TapeFlags flags);
        byte[] GetData();
    }
}
