using System.Xml;

namespace Kozynax.Lua
{
    /// <summary>
    /// Window-side operations a script needs while the emulator UI is open.
    /// </summary>
    public interface IScriptHost
    {
        void SetMaximumSpeed(bool enabled);
        void ShowError(string message);
        void Close();
        void LoadBus(XmlNode busNode);
    }
}
