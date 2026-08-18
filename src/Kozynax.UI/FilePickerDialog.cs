using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Kozui.Abstract;
using ZXMAK2.Dependency;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Host.WinForms.Lib.Layout;

namespace Kozynax.UI
{
    /// <summary>
    /// Kozui open/save file picker (ListView-backed; replaces Terminal FilePickerScreen).
    /// </summary>
    [KozuiDialog(CaptureBackdrop = true)]
    public sealed class FilePickerDialog : ViewDescription<FilePickerDialog>
    {
        private readonly bool _saveMode;
        private readonly bool _overwritePrompt;
        private readonly string _defaultExt;
        private readonly string _suggestedName;
        private readonly HashSet<string> _extensions;
        private string _directory;
        private List<Entry> _entries = new List<Entry>();

        public event EventHandler CloseRequested;

        public Panel Root { get; }
        public Label TitleLabel { get; }
        public Label PathLabel { get; }
        public ListView<Entry> EntriesList { get; }
        public Button OkButton { get; }
        public Button CancelButton { get; }
        public DlgResult DialogResult { get; private set; } = DlgResult.Cancel;
        public string FilePath { get; private set; }

        public FilePickerDialog(
            string title,
            string filter,
            bool saveMode,
            bool overwritePrompt = false,
            string defaultExt = null,
            string suggestedName = null)
        {
            _saveMode = saveMode;
            _overwritePrompt = overwritePrompt;
            _defaultExt = defaultExt;
            _suggestedName = string.IsNullOrWhiteSpace(suggestedName)
                ? string.Empty
                : Path.GetFileName(suggestedName.Trim());
            _extensions = ParseExtensions(filter);

            _directory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(_directory) || !Directory.Exists(_directory))
                _directory = Directory.GetCurrentDirectory();

            TitleLabel = new Label
            {
                Text = string.IsNullOrEmpty(title)
                    ? (saveMode ? "Save..." : "Open...")
                    : title,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            PathLabel = new Label
            {
                Text = _directory,
                HorizontalAlignment = HorizontalAlignment.Left,
                Margin = new Thickness(0, 1, 0, 0),
            };
            EntriesList = new ListView<Entry>
            {
                ItemTextSelector = FormatEntry,
                ActivateOnClick = false,
                ActivateOnSecondClick = true,
                Dock = Dock.Fill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                MinWidth = 40,
                MinHeight = 12,
                Margin = new Thickness(0, 1, 0, 1),
            };
            EntriesList.GetRowColors = GetEntryColors;
            OkButton = new Button { Text = saveMode ? "Save" : "Open" };
            CancelButton = new Button { Text = "Cancel" };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            buttons.Add(OkButton);
            buttons.Add(CancelButton);

            var help = new Label
            {
                Text = "Home/End  Enter/dbl-click open  Esc cancel",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 1, 0, 0),
            };

            var content = new DockPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(1),
            };
            TitleLabel.Dock = Dock.Top;
            PathLabel.Dock = Dock.Top;
            help.Dock = Dock.Bottom;
            buttons.Dock = Dock.Bottom;
            EntriesList.Dock = Dock.Fill;
            content.Add(TitleLabel);
            content.Add(PathLabel);
            content.Add(help);
            content.Add(buttons);
            content.Add(EntriesList);

            // Full-screen chrome (same idea as the old FilePickerScreen).
            var frame = new Placeholder
            {
                Content = content,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(0),
            };

            Root = new Panel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            Root.Add(frame);

            OkButton.Clicked += (_, __) => ActivateSelected();
            CancelButton.Clicked += (_, __) => Cancel();
            EntriesList.ItemActivated += (_, __) => ActivateSelected();

            ReloadEntries();
        }

        public static string PickOpen(string title, string filter)
        {
            var dialog = new FilePickerDialog(title, filter, saveMode: false);
            if (dialog.ShowDialog(null) != DlgResult.OK)
                return null;
            return dialog.FilePath;
        }

        public static string PickSave(
            string title,
            string filter,
            string defaultExt,
            string suggestedName,
            bool overwritePrompt)
        {
            var dialog = new FilePickerDialog(
                title,
                filter,
                saveMode: true,
                overwritePrompt,
                defaultExt,
                suggestedName);
            if (dialog.ShowDialog(null) != DlgResult.OK)
                return null;
            return dialog.FilePath;
        }

        public void Accept() => ActivateSelected();

        public void Cancel() => Complete(DlgResult.Cancel);

        private void ActivateSelected()
        {
            var entry = SelectedEntry;
            if (entry == null)
                return;

            if (entry.Kind == EntryKind.Parent)
            {
                var parent = Directory.GetParent(_directory);
                if (parent != null)
                {
                    _directory = parent.FullName;
                    ReloadEntries();
                }
                return;
            }

            if (entry.Kind == EntryKind.Directory)
            {
                _directory = entry.FullPath;
                ReloadEntries();
                return;
            }

            if (entry.Kind == EntryKind.NewFile)
            {
                if (!TryQueryFileName(out var name))
                    return;
                TryFinish(Path.Combine(_directory, name));
                return;
            }

            TryFinish(entry.FullPath);
        }

        private Entry SelectedEntry
        {
            get
            {
                var index = EntriesList.SelectedIndex;
                if (index < 0 || index >= _entries.Count)
                    return null;
                return _entries[index];
            }
        }

        private void TryFinish(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (_overwritePrompt && File.Exists(path))
            {
                var name = Path.GetFileName(path);
                var confirm = ConfirmDialog.ForButtonSet(
                    "Overwrite " + name + "?",
                    "Save",
                    DlgButtonSet.YesNo);
                if (confirm.ShowDialog(null) != DlgResult.Yes)
                    return;
            }

            FilePath = path;
            Complete(DlgResult.OK);
        }

        private bool TryQueryFileName(out string fileName)
        {
            fileName = null;
            var service = Locator.TryResolve<IUserQuery>();
            if (service == null)
                return false;

            var value = string.IsNullOrWhiteSpace(_suggestedName)
                ? SuggestDefaultName(_defaultExt)
                : _suggestedName;

            if (!service.QueryText("Save As", "File name:", ref value))
                return false;

            value = (value ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(value))
                return false;
            if (value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                Locator.TryResolve<IUserMessage>()?.Warning("Invalid file name.");
                return false;
            }

            fileName = EnsureExtension(value, _defaultExt);
            return true;
        }

        private void ReloadEntries()
        {
            PathLabel.Text = _directory ?? string.Empty;
            _entries = BuildEntries(_directory, _extensions, _saveMode);
            EntriesList.Reset(_entries);
            EntriesList.SelectedIndex = _entries.Count > 0 ? 0 : -1;
        }

        private void Complete(DlgResult result)
        {
            DialogResult = result;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private static string FormatEntry(Entry entry)
        {
            if (entry == null)
                return string.Empty;
            switch (entry.Kind)
            {
                case EntryKind.Parent:
                    return "[..]";
                case EntryKind.NewFile:
                    return "« New file… »";
                case EntryKind.Directory:
                    return "[" + entry.Name + "]";
                default:
                    return entry.Name ?? string.Empty;
            }
        }

        private ListRowColors? GetEntryColors(int index)
        {
            if (index < 0 || index >= _entries.Count)
                return null;
            var entry = _entries[index];
            if (entry.Kind == EntryKind.NewFile)
                return new ListRowColors(22, 26, 38, 240, 220, 120);
            if (entry.Kind == EntryKind.Directory || entry.Kind == EntryKind.Parent)
                return new ListRowColors(22, 26, 38, 140, 200, 255);
            return null;
        }

        private static List<Entry> BuildEntries(
            string directory,
            HashSet<string> extensions,
            bool saveMode)
        {
            var list = new List<Entry>();
            if (Directory.GetParent(directory) != null)
                list.Add(Entry.Parent());
            if (saveMode)
                list.Add(Entry.NewFile());

            try
            {
                foreach (var dir in Directory.EnumerateDirectories(directory)
                             .Select(Path.GetFileName)
                             .Where(n => !string.IsNullOrEmpty(n) && !n.StartsWith("."))
                             .OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
                {
                    list.Add(Entry.Directory(dir, Path.Combine(directory, dir)));
                }

                foreach (var file in Directory.EnumerateFiles(directory)
                             .Select(Path.GetFileName)
                             .Where(n => !string.IsNullOrEmpty(n) && !n.StartsWith(".") && Matches(n, extensions))
                             .OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
                {
                    list.Add(Entry.File(file, Path.Combine(directory, file)));
                }
            }
            catch (Exception ex)
            {
                global::ZXMAK2.Logger.Error(ex);
            }

            return list;
        }

        private static string SuggestDefaultName(string defaultExt)
        {
            if (string.IsNullOrWhiteSpace(defaultExt))
                return "snapshot";
            var ext = defaultExt.StartsWith(".") ? defaultExt : "." + defaultExt;
            return "snapshot" + ext;
        }

        private static string EnsureExtension(string fileName, string defaultExt)
        {
            if (string.IsNullOrWhiteSpace(fileName) || Path.HasExtension(fileName))
                return fileName;
            if (string.IsNullOrWhiteSpace(defaultExt))
                return fileName;
            var ext = defaultExt.StartsWith(".") ? defaultExt : "." + defaultExt;
            return fileName + ext;
        }

        private static bool Matches(string fileName, HashSet<string> extensions)
        {
            if (extensions == null || extensions.Count == 0)
                return true;
            var ext = Path.GetExtension(fileName);
            if (string.IsNullOrEmpty(ext))
                return extensions.Contains(string.Empty);
            return extensions.Contains(ext);
        }

        private static HashSet<string> ParseExtensions(string filter)
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(filter))
                return set;

            foreach (var part in filter.Split('|'))
            {
                if (!part.Contains("*"))
                    continue;
                foreach (var token in part.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var pattern = token.Trim();
                    if (pattern == "*.*" || pattern == "*")
                    {
                        set.Clear();
                        return set;
                    }
                    if (pattern.StartsWith("*."))
                        set.Add(pattern.Substring(1));
                }
            }
            return set;
        }

        public enum EntryKind
        {
            Parent,
            NewFile,
            Directory,
            File,
        }

        public sealed class Entry
        {
            private Entry(EntryKind kind, string name, string fullPath)
            {
                Kind = kind;
                Name = name;
                FullPath = fullPath;
            }

            public static Entry Parent() => new Entry(EntryKind.Parent, "..", null);
            public static Entry NewFile() => new Entry(EntryKind.NewFile, string.Empty, null);
            public static Entry Directory(string name, string fullPath)
                => new Entry(EntryKind.Directory, name, fullPath);
            public static Entry File(string name, string fullPath)
                => new Entry(EntryKind.File, name, fullPath);

            public EntryKind Kind { get; }
            public string Name { get; }
            public string FullPath { get; }
        }
    }
}
