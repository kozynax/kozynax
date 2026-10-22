using ZXMAK2.Engine.Cpu;
using ZXMAK2.Engine.Cpu.Processor;

namespace Kozynax.Lua
{
    /// <summary>
    /// Breakpoint argument. Lua reads the live registers as <c>cpu.regs.PC</c>.
    /// </summary>
    public sealed class LuaCpu
    {
        private Z80Cpu _cpu;

        public CpuRegs regs
        {
            get { return _cpu.regs; }
        }

        internal void Bind(Z80Cpu cpu)
        {
            _cpu = cpu;
        }
    }
}
