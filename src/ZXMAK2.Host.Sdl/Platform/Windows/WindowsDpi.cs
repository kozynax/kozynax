using System;
using System.Runtime.InteropServices;

namespace ZXMAK2.Host.SdlBackend.Platform.Windows
{
    /// <summary>
    /// Draw in physical pixels. Windows display scaling (125%, 150%, …) otherwise
    /// stretches the SDL framebuffer and softens the bitmap UI.
    /// </summary>
    internal static class WindowsDpi
    {
        // DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2
        private static readonly IntPtr PerMonitorAwareV2 = new IntPtr(-4);

        public static void IgnoreSystemScaling()
        {
            if (!OperatingSystem.IsWindows())
                return;

            try
            {
                if (SetProcessDpiAwarenessContext(PerMonitorAwareV2))
                    return;
            }
            catch (EntryPointNotFoundException)
            {
            }
            catch (DllNotFoundException)
            {
                return;
            }

            try
            {
                // PROCESS_PER_MONITOR_DPI_AWARE
                SetProcessDpiAwareness(2);
            }
            catch (EntryPointNotFoundException)
            {
                try
                {
                    SetProcessDPIAware();
                }
                catch (DllNotFoundException)
                {
                }
            }
            catch (DllNotFoundException)
            {
            }
        }

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern bool SetProcessDpiAwarenessContext(IntPtr value);

        [DllImport("shcore.dll", ExactSpelling = true)]
        private static extern int SetProcessDpiAwareness(int awareness);

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern bool SetProcessDPIAware();
    }
}
