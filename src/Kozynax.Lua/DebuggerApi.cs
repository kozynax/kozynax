using MoonSharp.Interpreter;

namespace Kozynax.Lua
{
    /// <summary>
    /// Lua <c>Debugger</c> table.
    /// </summary>
    public sealed class DebuggerApi
    {
        private readonly LuaScriptSession _session;

        internal DebuggerApi(LuaScriptSession session)
        {
            _session = session;
        }

        public void AddBreakpoint(int address, Closure callback)
        {
            _session.AddBreakpoint(address, callback);
        }

        // Raw bytes, so Lua compares with == and \xHH escapes.
        public string ReadMemory(int address, int length)
        {
            return _session.ReadMemory(address, length);
        }
    }
}
