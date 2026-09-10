namespace Kozynax.UI.Helpers
{
    /// <summary>
    /// Shared main-window caption formatting for WinForms and SDL hosts.
    /// </summary>
    public static class MainWindowTitle
    {
        public const string ProductName = "Kozynax";

        public static string Format(string fileTitle, bool isRunning)
        {
            var tail = isRunning ? ProductName : ProductName + " [paused]";
            return string.IsNullOrEmpty(fileTitle)
                ? tail
                : string.Format("[{0}] - {1}", fileTitle, tail);
        }
    }
}
