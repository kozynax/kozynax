using System;
using System.ComponentModel;
using Silk.NET.SDL;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.Terminal;

namespace ZXMAK2.Host.SdlBackend.Services
{
    public sealed class SdlUserMessage : IUserMessage
    {
        private readonly Sdl _sdl;

        public SdlUserMessage(Sdl sdl)
        {
            _sdl = sdl;
        }

        public void ErrorDetails(Exception ex)
            => Show(MessageBoxFlags.Error, "Error", ex?.ToString() ?? "Unknown error");

        public void Error(Exception ex)
            => Show(MessageBoxFlags.Error, "Error", ex?.Message ?? "Unknown error");

        public void Error(string fmt, params object[] args)
            => Show(MessageBoxFlags.Error, "Error", string.Format(fmt, args));

        public void Warning(Exception ex)
            => Show(MessageBoxFlags.Warning, "Warning", ex?.Message ?? "Unknown warning");

        public void Warning(string fmt, params object[] args)
            => Show(MessageBoxFlags.Warning, "Warning", string.Format(fmt, args));

        public void Info(string fmt, params object[] args)
            => Show(MessageBoxFlags.Information, "Info", string.Format(fmt, args));

        private unsafe void Show(MessageBoxFlags flags, string title, string message)
        {
            Logger.Error("{0}: {1}", title, message);
            _sdl.ShowSimpleMessageBox((uint)flags, title, message, null);
        }
    }

    public sealed class SdlUserQuery : IUserQuery
    {
        public DlgResult Show(string message, string caption, DlgButtonSet buttonSet, DlgIcon icon)
        {
            Console.WriteLine($"[{caption}] {message}");
            return DlgResult.OK;
        }

        public object ObjectSelector(object[] objArray, string caption)
            => objArray != null && objArray.Length > 0 ? objArray[0] : null;

        public bool QueryText(string caption, string text, ref string value)
            => false;

        public bool QueryValue(string caption, string text, string format, ref int value, int min, int max)
            => false;
    }

    public sealed class SdlUserHelp : IUserHelp
    {
        public bool CanShow(object uiControl) => false;
        public void ShowHelp(object uiControl) { }
        public void ShowHelp(object uiControl, string keyword) { }
    }

    public sealed class SdlOpenFileDialog : IOpenFileDialog
    {
        private readonly ITerminal _terminal;

        public SdlOpenFileDialog(ITerminal terminal)
        {
            _terminal = terminal;
            ReadOnlyChecked = true;
        }

        public event CancelEventHandler FileOk;
        public string Title { get; set; }
        public string Filter { get; set; }
        public string FileName { get; set; }
        public bool ShowReadOnly { get; set; }
        public bool ReadOnlyChecked { get; set; }
        public bool CheckFileExists { get; set; }
        public bool Multiselect { get; set; }

        public DlgResult ShowDialog(object owner)
        {
            var picker = new FilePickerScreen(_terminal);
            if (!picker.TryPickOpen(Title ?? "Open...", Filter, out var path))
                return DlgResult.Cancel;

            FileName = path;
            var args = new CancelEventArgs();
            FileOk?.Invoke(this, args);
            return args.Cancel ? DlgResult.Cancel : DlgResult.OK;
        }

        public void Dispose() { }
    }

    public sealed class SdlSaveFileDialog : ISaveFileDialog
    {
        public string Title { get; set; }
        public string Filter { get; set; }
        public string DefaultExt { get; set; }
        public string FileName { get; set; }
        public bool OverwritePrompt { get; set; }

        public DlgResult ShowDialog(object owner)
        {
            Console.WriteLine("Save file dialog is not available in SDL host.");
            return DlgResult.Cancel;
        }

        public void Dispose() { }
    }
}
