using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ZXMAK2.Host.Terminal
{
    /// <summary>
    /// Backend-neutral open-file overlay. Renders through <see cref="ITerminal"/> only.
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
            if (!_terminal.IsAvailable)
                return false;

            var extensions = ParseExtensions(filter);
            var directory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
                directory = Directory.GetCurrentDirectory();

            var selected = 0;
            var scroll = 0;
            var entries = BuildEntries(directory, extensions);

            while (true)
            {
                while (_terminal.PollEvent(out var ev))
                {
                    if (ev.Kind == TerminalEventKind.Quit)
                        return false;

                    if (ev.Kind != TerminalEventKind.KeyDown)
                        continue;

                    if (!OnKey(ev.Key, ref selected, ref scroll, ref directory, ref entries, extensions, out filePath, out var done))
                        return false;
                    if (done)
                        return !string.IsNullOrEmpty(filePath);
                }

                Draw(title ?? "Open...", directory, entries, selected, scroll);
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
            out string filePath,
            out bool done)
        {
            filePath = null;
            done = false;

            // Navigation keys must work even when the listing is empty.
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
                    entries = BuildEntries(directory, extensions);
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
                if (entry.IsDirectory)
                {
                    directory = entry.FullPath;
                    entries = BuildEntries(directory, extensions);
                    selected = 0;
                    scroll = 0;
                    return true;
                }

                filePath = entry.FullPath;
                done = true;
                return true;
            }

            return true;
        }

        private void Draw(string title, string directory, List<Entry> entries, int selected, int scroll)
        {
            var winW = _terminal.Width;
            var winH = _terminal.Height;
            _terminal.Clear(TerminalColor.Rgb(16, 18, 24));

            // Spectrum 8x8 looks right at 1x; 2x overflows help text on a 640-wide window.
            const int scale = 1;
            const int pad = 8;
            var lineH = TerminalFont.GlyphHeight * scale + 2;
            var y = pad;
            var cols = Math.Max(1, (winW - pad * 2) / (TerminalFont.GlyphWidth * scale));

            _terminal.DrawText(pad, y, Truncate(title, cols), scale, TerminalColor.Rgb(240, 220, 120));
            y += lineH + 2;
            _terminal.DrawText(pad, y, Truncate(directory, cols), scale, TerminalColor.Rgb(160, 170, 190));
            y += lineH + 4;

            _terminal.DrawText(
                pad,
                winH - pad - TerminalFont.GlyphHeight * scale,
                Truncate("Up/Dn PgUp/Dn Enter  Bksp=up  Esc=cancel", cols),
                scale,
                TerminalColor.Rgb(110, 120, 140));

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

                var label = entry.IsDirectory ? "[" + entry.Name + "]" : entry.Name;
                _terminal.DrawText(
                    pad,
                    rowY,
                    Truncate(label, cols),
                    scale,
                    entry.IsDirectory ? TerminalColor.Rgb(140, 200, 255) : TerminalColor.Rgb(230, 230, 230));
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

        private static List<Entry> BuildEntries(string directory, HashSet<string> extensions)
        {
            var list = new List<Entry>();
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

        private readonly struct Entry
        {
            public Entry(string name, string fullPath, bool isDirectory)
            {
                Name = name;
                FullPath = fullPath;
                IsDirectory = isDirectory;
            }

            public string Name { get; }
            public string FullPath { get; }
            public bool IsDirectory { get; }
        }
    }
}
