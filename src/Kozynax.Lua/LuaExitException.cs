using System;

namespace Kozynax.Lua
{
    /// <summary>
    /// Thrown by <c>Emulator.Exit</c> so the rest of the script does not run.
    /// </summary>
    sealed class LuaExitException : Exception
    {
        public LuaExitException(int code)
            : base("script exit " + code)
        {
            Code = code;
        }

        public int Code { get; }
    }
}
