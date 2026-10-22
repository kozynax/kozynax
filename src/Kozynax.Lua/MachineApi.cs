namespace Kozynax.Lua
{
    /// <summary>
    /// Lua <c>Machine</c> table.
    /// </summary>
    public sealed class MachineApi
    {
        private readonly LuaScriptSession _session;

        internal MachineApi(LuaScriptSession session)
        {
            _session = session;
        }

        public void Set(string name)
        {
            _session.SetMachine(name);
        }

        public void Reset()
        {
            _session.Reset();
        }
    }
}
