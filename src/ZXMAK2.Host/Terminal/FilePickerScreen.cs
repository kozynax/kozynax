using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZXMAK2.Dependency;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Interfaces;

namespace ZXMAK2.Host.Terminal
{
    /// <summary>
    /// Backend-neutral open/save file overlay. Renders through <see cref="ITerminal"/> only.
    /// </summary>
    public sealed class FilePickerScreen
    {
        private readonly ITerminal _terminal;

        public FilePickerScreen(ITerminal terminal)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        }

        public bool TryPickOpen(string title, string filter, out string filePath)
        {
            filePath = null;
            return Run(
                title ?? "Open...",
                filter,
                defaultExt: null,
                overwritePrompt: false,
                saveMode: false,
                out filePath);
        }

        public bool TryPickSave(
            string title,
            string filter,
            string defaultExt,
            string suggestedName,
            bool overwritePrompt,
            out string filePath)
        {
            filePath = null;
            return Run(
                title ?? "Save...",
                filter,
                defaultExt,
                overwritePrompt,
                saveMode: true,
                out filePath,
                suggestedName);
        }

        private bool Run(
            string title,
            string filter,
            string defaultExt,
            bool overwritePrompt,
            bool saveMode,
            out string filePath,
            string suggestedName = null)
        {
            filePath = null;
            if (!_terminal.IsAvailable)
                return false;

            var extensions = ParseExtensions(filter);
            var directory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                directory = Directory.GetCurrentDirectory();

            var selected = 0;
            var scroll = 0;
            var entries = BuildEntries(directory, extensions, saveMode);
            var mode = Mode.Browse;
            string pendingPath = null;
            var suggested = string.IsNullOrWhiteSpace(suggestedName)
                ? string.Empty
                : Path.GetFileName(suggestedName.Trim());

            while (true)
            {
                while (_terminal.PollEvent(out var ev))
                {
                    if (ev.Kind == TerminalEventKind.Quit)
                        return false;

                    if (ev.Kind != TerminalEventKind.KeyDown)
                        continue;

                    if (!OnKey(
                            ev.Key,
                            ref selected,
                            ref scroll,
                            ref directory,
                            ref entries,
                            extensions,
                            saveMode,
                            overwritePrompt,
                            defaultExt,
                            suggested,
                            ref mode,
                            ref pendingPath,
                            out filePath,
                            out var done))
                        return false;
                    if (done)
                        return !string.IsNullOrEmpty(filePath);
                }

                Draw(title, directory, entries, selected, scroll, saveMode, mode, pendingPath);
                TerminalUiSession.AfterFrame(_terminal);
            }
        }

        private bool OnKey(
            TerminalKey key,
            ref int selected,
            ref int scroll,
            ref string directory,
            ref List<Entry> entries,
            HashSet<string> extensions,
            bool saveMode,
            bool overwritePrompt,
            string defaultExt,
            string suggestedName,
            ref Mode mode,
            ref string pendingPath,
            out string filePath,
            out bool done)
        {
            filePath = null;
            done = false;

            if (mode == Mode.ConfirmOverwrite)
            {
                if (key == TerminalKey.Escape || key == TerminalKey.N)
                {
                    mode = Mode.Browse;
                    pendingPath = null;
                    return true;
                }

                if (key == TerminalKey.Y || key == TerminalKey.Enter)
                {
                    filePath = pendingPath;
                    pendingPath = null;
                    done = true;
                    return true;
                }

                return true;
            }

            if (key == TerminalKey.Escape)
            {
                done = true;
                return true;
            }

            if (key == TerminalKey.Backspace || key == TerminalKey.Left)
            {
                var parent = Directory.GetParent(directory);
                if (parent != null)
                {
                    directory = parent.FullName;
                    entries = BuildEntries(directory, extensions, saveMode);
                    selected = 0;
                    scroll = 0;
                }
                return true;
            }

            if (entries == null || entries.Count == 0)
                return true;

            if (key == TerminalKey.Up)
            {
                selected = Math.Max(0, selected - 1);
                EnsureVisible(selected, ref scroll, VisibleRows());
                return true;
            }

            if (key == TerminalKey.Down)
            {
                selected = Math.Min(entries.Count - 1, selected + 1);
                EnsureVisible(selected, ref scroll, VisibleRows());
                return true;
            }

            if (key == TerminalKey.PageUp)
            {
                selected = Math.Max(0, selected - VisibleRows());
                EnsureVisible(selected, ref scroll, VisibleRows());
                return true;
            }

            if (key == TerminalKey.PageDown)
            {
                selected = Math.Min(entries.Count - 1, selected + VisibleRows());
                EnsureVisible(selected, ref scroll, VisibleRows());
                return true;
            }

            if (key == TerminalKey.Enter || key == TerminalKey.Right)
            {
                var entry = entries[selected];
                if (entry.IsNewFile)
                {
                    if (!TryQueryFileName(suggestedName, defaultExt, out var name))
                        return true;
                    var path = Path.Combine(directory, name);
                    return AcceptPath(
                        path,
                        overwritePrompt,
                        ref mode,
                        ref pendingPath,
                        out filePath,
                        out done);
                }

                if (entry.IsDirectory)
                {
                    directory = entry.FullPath;
                    entries = BuildEntries(directory, extensions, saveMode);
                    selected = 0;
                    scroll = 0;
                    return true;
                }

                return AcceptPath(
                    entry.FullPath,
                    saveMode && overwritePrompt,
                    ref mode,
                    ref pendingPath,
                    out filePath,
                    out done);
            }

            return true;
        }

        private static bool TryQueryFileName(string suggestedName, string defaultExt, out string fileName)
        {
            fileName = null;
            var service = Locator.TryResolve<IUserQuery>();
            if (service == null)
                return false;

            var value = string.IsNullOrWhiteSpace(suggestedName)
                ? SuggestDefaultName(defaultExt)
                : suggestedName.Trim();

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

            fileName = EnsureExtension(value, defaultExt);
            return true;
        }

        private static string SuggestDefaultName(string defaultExt)
        {
            if (string.IsNullOrWhiteSpace(defaultExt))
                return "snapshot";
            var ext = defaultExt.StartsWith(".") ? defaultExt : "." + defaultExt;
            return "snapshot" + ext;
        }

        private static bool AcceptPath(
            string path,
            bool overwritePrompt,
            ref Mode mode,
            ref string pendingPath,
            out string filePath,
            out bool done)
        {
            filePath = null;
            done = false;
            if (string.IsNullOrEmpty(path))
                return true;

            if (overwritePrompt && File.Exists(path))
            {
                pendingPath = path;
                mode = Mode.ConfirmOverwrite;
                return true;
            }

            filePath = path;
            done = true;
            return true;
        }

        private void Draw(
            string title,
            string directory,
            List<Entry> entries,
            int selected,
            int scroll,
            bool saveMode,
            Mode mode,
            string pendingPath)
        {
            var winW = _terminal.Width;
            var winH = _terminal.Height;
            _terminal.Clear(TerminalColor.Rgb(16, 18, 24));

            const int scale = 1;
            const int pad = 8;
            var lineH = TerminalFont.GlyphHeight * scale + 2;
            var y = pad;
            var cols = Math.Max(1, (winW - pad * 2) / (TerminalFont.GlyphWidth * scale));

            _terminal.DrawText(pad, y, Truncate(title, cols), scale, TerminalColor.Rgb(240, 220, 120));
            y += lineH + 2;
            _terminal.DrawText(pad, y, Truncate(directory, cols), scale, TerminalColor.Rgb(160, 170, 190));
            y += lineH + 4;

            string help;
            if (mode == Mode.ConfirmOverwrite)
                help = "Overwrite " + Path.GetFileName(pendingPath) + "?  Y/Enter=yes  N/Esc=no";
            else if (saveMode)
                help = "Up/Dn Enter  Bksp=up  Esc=cancel  (« New file… » to name)";
            else
                help = "Up/Dn PgUp/Dn Enter  Bksp=up  Esc=cancel";

            _terminal.DrawText(
                pad,
                winH - pad - TerminalFont.GlyphHeight * scale,
                Truncate(help, cols),
                scale,
                TerminalColor.Rgb(110, 120, 140));

            if (mode == Mode.ConfirmOverwrite)
            {
                _terminal.Present();
                return;
            }

            var rows = Math.Max(1, (winH - y - pad - lineH) / lineH);
            if (entries == null)
                entries = new List<Entry>();

            for (var i = 0; i < rows && scroll + i < entries.Count; i++)
            {
                var index = scroll + i;
                var entry = entries[index];
                var rowY = y + i * lineH;
                if (index == selected)
                    _terminal.FillRect(pad - 2, rowY - 1, winW - pad * 2 + 4, lineH, TerminalColor.Rgb(40, 70, 120));

                string label;
                TerminalColor color;
                if (entry.IsNewFile)
                {
                    label = "« New file… »";
                    color = TerminalColor.Rgb(240, 220, 120);
                }
                else if (entry.IsDirectory)
                {
                    label = "[" + entry.Name + "]";
                    color = TerminalColor.Rgb(140, 200, 255);
                }
                else
                {
                    label = entry.Name;
                    color = TerminalColor.Rgb(230, 230, 230);
                }

                _terminal.DrawText(pad, rowY, Truncate(label, cols), scale, color);
            }

            _terminal.Present();
        }

        private int VisibleRows()
        {
            const int scale = 1;
            const int pad = 8;
            var lineH = TerminalFont.GlyphHeight * scale + 2;
            var top = pad + (lineH + 2) + (lineH + 4);
            return Math.Max(1, (_terminal.Height - top - pad - lineH) / lineH);
        }

        private static void EnsureVisible(int selected, ref int scroll, int rows)
        {
            if (selected < scroll)
                scroll = selected;
            else if (selected >= scroll + rows)
                scroll = selected - rows + 1;
            if (scroll < 0)
                scroll = 0;
        }

        private static List<Entry> BuildEntries(string directory, HashSet<string> extensions, bool saveMode)
        {
            var list = new List<Entry>();
            if (saveMode)
                list.Add(Entry.NewFile());

            try
            {
                foreach (var dir in Directory.EnumerateDirectories(directory)
                             .Select(Path.GetFileName)
                             .Where(n => !string.IsNullOrEmpty(n) && !n.StartsWith("."))
                             .OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
                {
                    list.Add(new Entry(dir, Path.Combine(directory, dir), true));
                }

                foreach (var file in Directory.EnumerateFiles(directory)
                             .Select(Path.GetFileName)
                             .Where(n => !string.IsNullOrEmpty(n) && !n.StartsWith(".") && Matches(n, extensions))
                             .OrderBy(n => n, StringComparer.OrdinalIgnoreCase))
                {
                    list.Add(new Entry(file, Path.Combine(directory, file), false));
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }

            return list;
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

        private static string Truncate(string text, int maxChars)
        {
            if (string.IsNullOrEmpty(text) || maxChars <= 0)
                return string.Empty;
            if (text.Length <= maxChars)
                return text;
            if (maxChars <= 3)
                return text.Substring(0, maxChars);
            return text.Substring(0, maxChars - 3) + "...";
        }

        private enum Mode
        {
            Browse,
            ConfirmOverwrite,
        }

        private readonly struct Entry
        {
            public Entry(string name, string fullPath, bool isDirectory)
            {
                Name = name;
                FullPath = fullPath;
                IsDirectory = isDirectory;
                IsNewFile = false;
            }

            private Entry(bool isNewFile)
            {
                Name = string.Empty;
                FullPath = string.Empty;
                IsDirectory = false;
                IsNewFile = isNewFile;
            }

            public static Entry NewFile() => new Entry(true);

            public string Name { get; }
            public string FullPath { get; }
            public bool IsDirectory { get; }
            public bool IsNewFile { get; }
        }
    }
}
