using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace ZXMAK2.Host.SdlBackend.Services
{
    /// <summary>
    /// Minimal INI reader/writer (sections, key=value, ;/# comments).
    /// </summary>
    internal sealed class IniFile
    {
        private readonly string _path;
        private readonly Dictionary<string, Dictionary<string, string>> _sections =
            new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        private readonly List<string> _sectionOrder = new List<string>();

        public IniFile(string path)
        {
            _path = path ?? throw new ArgumentNullException(nameof(path));
        }

        public void Load()
        {
            _sections.Clear();
            _sectionOrder.Clear();
            if (!File.Exists(_path))
                return;

            string section = null;
            foreach (var raw in File.ReadAllLines(_path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#"))
                    continue;
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    section = line.Substring(1, line.Length - 2).Trim();
                    EnsureSection(section);
                    continue;
                }
                if (section == null)
                    continue;

                var eq = line.IndexOf('=');
                if (eq <= 0)
                    continue;
                var key = line.Substring(0, eq).Trim();
                var value = line.Substring(eq + 1).Trim();
                if (key.Length == 0)
                    continue;
                EnsureSection(section)[key] = value;
            }
        }

        public void Save()
        {
            var text = new StringBuilder();
            foreach (var section in _sectionOrder)
            {
                if (!_sections.TryGetValue(section, out var keys) || keys.Count == 0)
                    continue;
                text.Append('[').Append(section).Append(']').AppendLine();
                foreach (var pair in keys)
                    text.Append(pair.Key).Append('=').Append(pair.Value).AppendLine();
                text.AppendLine();
            }
            File.WriteAllText(_path, text.ToString());
        }

        public int GetInt(string section, string key, int fallback)
        {
            if (!TryGet(section, key, out var text))
                return fallback;
            if (!int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number))
                return fallback;
            return number;
        }

        public void SetInt(string section, string key, int value)
        {
            EnsureSection(section)[key] = value.ToString(CultureInfo.InvariantCulture);
        }

        private bool TryGet(string section, string key, out string value)
        {
            value = null;
            if (!_sections.TryGetValue(section, out var keys))
                return false;
            return keys.TryGetValue(key, out value);
        }

        private Dictionary<string, string> EnsureSection(string section)
        {
            if (_sections.TryGetValue(section, out var keys))
                return keys;
            keys = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            _sections[section] = keys;
            _sectionOrder.Add(section);
            return keys;
        }
    }
}
