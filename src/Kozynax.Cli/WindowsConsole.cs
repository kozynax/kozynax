using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Kozynax.Cli
{
    /// <summary>
    /// WinExe builds have no console by default. Attach the parent console
    /// (or allocate one) so CLI / TUI output is visible when needed.
    /// </summary>
    internal static class WindowsConsole
    {
        private const int AttachParentProcess = -1;
        private const int StdInputHandle = -10;
        private const int StdOutputHandle = -11;
        private const int StdErrorHandle = -12;
        private const uint GenericRead = 0x80000000;
        private const uint GenericWrite = 0x40000000;
        private const uint FileShareRead = 0x00000001;
        private const uint FileShareWrite = 0x00000002;
        private const uint OpenExisting = 3;
        private const uint FileTypeDisk = 0x0001;
        private const uint FileTypePipe = 0x0003;
        private const uint EnableProcessedOutput = 0x0001;
        private const uint EnableVirtualTerminalProcessing = 0x0004;
        private const uint EnableProcessedInput = 0x0001;
        private const uint EnableWindowInput = 0x0008;
        private const uint EnableMouseInput = 0x0010;
        private const uint EnableQuickEditMode = 0x0040;
        private const uint EnableExtendedFlags = 0x0080;

        private static readonly IntPtr InvalidHandle = new IntPtr(-1);

        /// <param name="exclusiveInput">
        /// True for <c>--tui</c> / <c>--console</c>: allocate our own console so
        /// cmd.exe does not keep the prompt and swallow keys. False attaches to
        /// the parent console for one-shot output such as <c>convert</c>.
        /// </param>
        public static void EnsureAttached(bool exclusiveInput = false)
        {
#if NETFRAMEWORK
            // WinForms host is Windows-only.
#else
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;
#endif

            // WinExe std handles are null; .NET reports that as "redirected".
            // Only skip when stdout/stderr already go to a real file or pipe.
            if (IsFileOrPipe(GetStdHandle(StdOutputHandle))
                || IsFileOrPipe(GetStdHandle(StdErrorHandle)))
                return;

            if (exclusiveInput)
            {
                // WinExe does not make cmd.exe wait, so AttachConsole shares the
                // prompt: output works, ReadKey never sees keys. Own the console.
                if (GetConsoleWindow() != IntPtr.Zero)
                    FreeConsole();
                AllocConsole();
            }
            else if (GetConsoleWindow() == IntPtr.Zero)
            {
                if (!AttachConsole(AttachParentProcess))
                    AllocConsole();
            }

            BindToConsoleDevice();

            if (exclusiveInput)
                TryFocus();
        }

        /// <summary>Bring this process's console to the foreground (TUI input).</summary>
        public static void TryFocus()
        {
#if !NETFRAMEWORK
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return;
#endif
            var console = GetConsoleWindow();
            if (console != IntPtr.Zero)
                SetForegroundWindow(console);
        }

        private static void BindToConsoleDevice()
        {
            // AttachConsole does not assign std handles. Open CONIN$/CONOUT$
            // so Console.WriteLine / ReadKey reach the attached or allocated console.
            var output = CreateFile(
                "CONOUT$",
                GenericRead | GenericWrite,
                FileShareRead | FileShareWrite,
                IntPtr.Zero,
                OpenExisting,
                0,
                IntPtr.Zero);
            var input = CreateFile(
                "CONIN$",
                GenericRead | GenericWrite,
                FileShareRead | FileShareWrite,
                IntPtr.Zero,
                OpenExisting,
                0,
                IntPtr.Zero);

            if (output != IntPtr.Zero && output != InvalidHandle)
            {
                SetStdHandle(StdOutputHandle, output);
                SetStdHandle(StdErrorHandle, output);
            }
            if (input != IntPtr.Zero && input != InvalidHandle)
                SetStdHandle(StdInputHandle, input);

            EnableVirtualTerminal(GetStdHandle(StdOutputHandle));
            EnableVirtualTerminal(GetStdHandle(StdErrorHandle));
            ConfigureInput(GetStdHandle(StdInputHandle));

            var encoding = Console.OutputEncoding ?? new UTF8Encoding(false);
            Console.SetOut(new StreamWriter(Console.OpenStandardOutput(), encoding) { AutoFlush = true });
            Console.SetError(new StreamWriter(Console.OpenStandardError(), encoding) { AutoFlush = true });
            Console.SetIn(new StreamReader(Console.OpenStandardInput(), Console.InputEncoding ?? encoding));
        }

        private static bool IsFileOrPipe(IntPtr handle)
        {
            if (handle == IntPtr.Zero || handle == InvalidHandle)
                return false;
            var fileType = GetFileType(handle);
            return fileType == FileTypeDisk || fileType == FileTypePipe;
        }

        private static void EnableVirtualTerminal(IntPtr handle)
        {
            if (handle == IntPtr.Zero || handle == InvalidHandle)
                return;
            uint mode;
            if (!GetConsoleMode(handle, out mode))
                return;
            SetConsoleMode(handle, mode | EnableProcessedOutput | EnableVirtualTerminalProcessing);
        }

        private static void ConfigureInput(IntPtr handle)
        {
            if (handle == IntPtr.Zero || handle == InvalidHandle)
                return;
            uint mode;
            if (!GetConsoleMode(handle, out mode))
                return;

            // ENABLE_EXTENDED_FLAGS is required to change Quick Edit. Quick Edit
            // and mouse selection pause ReadConsoleInput, so Console.ReadKey blocks.
            mode &= ~EnableQuickEditMode;
            mode &= ~EnableMouseInput;
            mode |= EnableExtendedFlags | EnableProcessedInput | EnableWindowInput;
            SetConsoleMode(handle, mode);
            FlushConsoleInputBuffer(handle);
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AttachConsole(int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool AllocConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FreeConsole();

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetStdHandle(int nStdHandle, IntPtr hHandle);

        [DllImport("kernel32.dll")]
        private static extern uint GetFileType(IntPtr hFile);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool FlushConsoleInputBuffer(IntPtr hConsoleInput);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);
    }
}
