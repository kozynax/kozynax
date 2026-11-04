using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Kozynax.UI;
using ZXMAK2.Host.AvaloniaBackend.Views;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Interfaces;

namespace ZXMAK2.Host.AvaloniaBackend.Services
{
    public sealed class AvaloniaUserMessage : IUserMessage
    {
        private readonly AvaloniaRuntimeContext _runtime;

        public AvaloniaUserMessage(AvaloniaRuntimeContext runtime)
        {
            _runtime = runtime;
        }

        public void ErrorDetails(Exception ex)
            => Show("Error", ex?.ToString() ?? "Unknown error");

        public void Error(Exception ex)
            => Show("Error", ex?.Message ?? "Unknown error");

        public void Error(string fmt, params object[] args)
            => Show("Error", string.Format(fmt, args));

        public void Warning(Exception ex)
            => Show("Warning", ex?.Message ?? "Unknown warning");

        public void Warning(string fmt, params object[] args)
            => Show("Warning", string.Format(fmt, args));

        public void Info(string fmt, params object[] args)
            => Show("Info", string.Format(fmt, args));

        private void Show(string title, string message)
        {
            Logger.Error("{0}: {1}", title, message);
            ModalScope.Run(_runtime, () => MessageWindow.Show(_runtime.MainWindow, title, message, DlgButtonSet.OK));
        }
    }

    public sealed class AvaloniaUserQuery : IUserQuery
    {
        private readonly AvaloniaRuntimeContext _runtime;

        public AvaloniaUserQuery(AvaloniaRuntimeContext runtime)
        {
            _runtime = runtime;
        }

        public DlgResult Show(string message, string caption, DlgButtonSet buttonSet, DlgIcon icon)
            => ModalScope.Run(_runtime, () => MessageWindow.Show(_runtime.MainWindow, caption, message, buttonSet));

        // Kozui-backed until the Avalonia Kozui presenter lands.
        public object ObjectSelector(object[] objArray, string caption)
            => ObjectSelectorDialog.Select(objArray, caption);

        public bool QueryText(string caption, string text, ref string value)
            => InputDialog.Query(caption, text, ref value);

        public bool QueryValue(string caption, string text, string format, ref int value, int min, int max)
            => InputDialog.QueryValue(caption, text, format, ref value, min, max);
    }

    public sealed class AvaloniaUserHelp : IUserHelp
    {
        public bool CanShow(object uiControl) => false;
        public void ShowHelp(object uiControl) { }
        public void ShowHelp(object uiControl, string keyword) { }
    }

    public sealed class AvaloniaOpenFileDialog : IOpenFileDialog
    {
        private readonly AvaloniaRuntimeContext _runtime;

        public AvaloniaOpenFileDialog(AvaloniaRuntimeContext runtime)
        {
            _runtime = runtime;
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
            var window = _runtime.MainWindow;
            if (window == null)
                return DlgResult.Cancel;

            var path = ModalScope.Run(_runtime, () => AvaloniaRuntimeContext.RunSync(async () =>
            {
                var options = new FilePickerOpenOptions
                {
                    Title = Title ?? "Open...",
                    AllowMultiple = false,
                    FileTypeFilter = FileFilter.Parse(Filter),
                };
                var start = await FileFilter.StartFolderAsync(window, FileName);
                if (start != null)
                    options.SuggestedStartLocation = start;
                var files = await window.StorageProvider.OpenFilePickerAsync(options);
                return files.Count > 0 ? files[0].TryGetLocalPath() : null;
            }));

            if (string.IsNullOrEmpty(path))
                return DlgResult.Cancel;

            FileName = path;
            var args = new CancelEventArgs();
            FileOk?.Invoke(this, args);
            return args.Cancel ? DlgResult.Cancel : DlgResult.OK;
        }

        public void Dispose() { }
    }

    public sealed class AvaloniaSaveFileDialog : ISaveFileDialog
    {
        private readonly AvaloniaRuntimeContext _runtime;

        public AvaloniaSaveFileDialog(AvaloniaRuntimeContext runtime)
        {
            _runtime = runtime;
            OverwritePrompt = true;
        }

        public string Title { get; set; }
        public string Filter { get; set; }
        public string DefaultExt { get; set; }
        public string FileName { get; set; }
        public bool OverwritePrompt { get; set; }

        public DlgResult ShowDialog(object owner)
        {
            var window = _runtime.MainWindow;
            if (window == null)
                return DlgResult.Cancel;

            var path = ModalScope.Run(_runtime, () => AvaloniaRuntimeContext.RunSync(async () =>
            {
                var options = new FilePickerSaveOptions
                {
                    Title = Title ?? "Save...",
                    FileTypeChoices = FileFilter.Parse(Filter),
                    DefaultExtension = string.IsNullOrEmpty(DefaultExt) ? null : DefaultExt.TrimStart('.'),
                    ShowOverwritePrompt = OverwritePrompt,
                    SuggestedFileName = string.IsNullOrEmpty(FileName) ? null : Path.GetFileName(FileName),
                };
                var start = await FileFilter.StartFolderAsync(window, FileName);
                if (start != null)
                    options.SuggestedStartLocation = start;
                var file = await window.StorageProvider.SaveFilePickerAsync(options);
                return file?.TryGetLocalPath();
            }));

            if (string.IsNullOrEmpty(path))
                return DlgResult.Cancel;

            FileName = path;
            return DlgResult.OK;
        }

        public void Dispose() { }
    }

    /// <summary>Quiesce emulator input around a modal (keys would otherwise stick).</summary>
    internal static class ModalScope
    {
        public static T Run<T>(AvaloniaRuntimeContext runtime, Func<T> body)
        {
            runtime?.PrepareUiInput?.Invoke();
            try
            {
                return body();
            }
            finally
            {
                runtime?.EndUiInput?.Invoke();
            }
        }
    }

    /// <summary>WinForms-style "Name|*.a;*.b|Name2|*.c" filter → Avalonia file types.</summary>
    internal static class FileFilter
    {
        public static List<FilePickerFileType> Parse(string filter)
        {
            var result = new List<FilePickerFileType>();
            if (string.IsNullOrWhiteSpace(filter))
                return result;

            var parts = filter.Split('|');
            for (var i = 0; i + 1 < parts.Length; i += 2)
            {
                var patterns = parts[i + 1]
                    .Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => p.Length > 0)
                    .ToList();
                if (patterns.Count == 0)
                    continue;
                result.Add(new FilePickerFileType(parts[i].Trim()) { Patterns = patterns });
            }
            return result;
        }

        public static async System.Threading.Tasks.Task<IStorageFolder> StartFolderAsync(TopLevel top, string fileName)
        {
            try
            {
                if (string.IsNullOrEmpty(fileName))
                    return null;
                var dir = Directory.Exists(fileName) ? fileName : Path.GetDirectoryName(fileName);
                if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir))
                    return null;
                return await top.StorageProvider.TryGetFolderFromPathAsync(dir);
            }
            catch
            {
                return null;
            }
        }
    }
}
