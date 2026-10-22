namespace Kozynax.Lua
{
    /// <summary>
    /// Lua <c>Tape</c> table.
    /// </summary>
    public sealed class TapeApi
    {
        private readonly LuaScriptSession _session;

        internal TapeApi(LuaScriptSession session)
        {
            _session = session;
        }

        public bool AutoPlay
        {
            get { return _session.TapeDevice.UseAutoPlay; }
            set { _session.TapeDevice.UseAutoPlay = value; }
        }

        public bool UseTraps
        {
            get { return _session.TapeDevice.UseTraps; }
            set { _session.TapeDevice.UseTraps = value; }
        }

        public void Start()
        {
            _session.TapeDevice.Play();
        }
    }
}
