using System;
using System.Collections.Generic;

namespace Kozynax.UI
{
    /// <summary>
    /// Collapses machine names that share a word prefix into one submenu.
    /// "ZX Spectrum 48" and "ZX Spectrum 128" become "ZX Spectrum" → "48", "128".
    /// </summary>
    public static class MachineMenuGrouping
    {
        public sealed class Entry
        {
            public Entry(string caption, MachineSettings.MachineConfiguration machine, IReadOnlyList<Entry> children)
            {
                Caption = caption ?? string.Empty;
                Machine = machine;
                Children = children ?? Array.Empty<Entry>();
            }

            public string Caption { get; }
            public MachineSettings.MachineConfiguration Machine { get; }
            public IReadOnlyList<Entry> Children { get; }
            public bool IsGroup => Children.Count > 0;
        }

        public static IReadOnlyList<Entry> Group(IEnumerable<MachineSettings.MachineConfiguration> machines)
        {
            var list = new List<MachineSettings.MachineConfiguration>();
            if (machines != null)
            {
                foreach (var machine in machines)
                {
                    if (machine != null)
                        list.Add(machine);
                }
            }

            var words = new string[list.Count][];
            for (var i = 0; i < list.Count; i++)
                words[i] = SplitWords(list[i].Name);

            var prefixLen = new int[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                var best = 0;
                for (var j = 0; j < list.Count; j++)
                {
                    if (i == j)
                        continue;
                    var n = SharedPrefixLength(words[i], words[j]);
                    if (n > best)
                        best = n;
                }
                prefixLen[i] = best;
            }

            var result = new List<Entry>();
            var emitted = new bool[list.Count];
            for (var i = 0; i < list.Count; i++)
            {
                if (emitted[i])
                    continue;

                if (prefixLen[i] <= 0)
                {
                    result.Add(Leaf(list[i], CaptionOf(words[i], 0)));
                    emitted[i] = true;
                    continue;
                }

                var key = Prefix(words[i], prefixLen[i]);
                var members = new List<int>();
                for (var j = i; j < list.Count; j++)
                {
                    if (emitted[j] || prefixLen[j] != prefixLen[i])
                        continue;
                    if (!string.Equals(Prefix(words[j], prefixLen[j]), key, StringComparison.Ordinal))
                        continue;
                    members.Add(j);
                }

                if (members.Count < 2)
                {
                    result.Add(Leaf(list[i], CaptionOf(words[i], 0)));
                    emitted[i] = true;
                    continue;
                }

                var children = new List<Entry>(members.Count);
                foreach (var j in members)
                {
                    emitted[j] = true;
                    children.Add(Leaf(list[j], CaptionOf(words[j], prefixLen[j])));
                }
                result.Add(new Entry(key, null, children));
            }

            return result;
        }

        private static Entry Leaf(MachineSettings.MachineConfiguration machine, string caption)
            => new Entry(caption, machine, Array.Empty<Entry>());

        private static string[] SplitWords(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return Array.Empty<string>();
            return name.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Longest common word prefix that still leaves a suffix on both names.
        /// "Scorpion ZS 256" and "Scorpion ZS 256 PROF-ROM" share "Scorpion ZS".
        /// </summary>
        private static int SharedPrefixLength(string[] a, string[] b)
        {
            var limit = Math.Min(a.Length, b.Length);
            var n = 0;
            while (n < limit && string.Equals(a[n], b[n], StringComparison.Ordinal))
                n++;
            if (n >= a.Length || n >= b.Length)
                n = limit - 1;
            return n < 0 ? 0 : n;
        }

        private static string Prefix(string[] words, int count)
            => string.Join(" ", words, 0, Math.Max(0, Math.Min(count, words.Length)));

        private static string CaptionOf(string[] words, int prefixLen)
        {
            if (words.Length == 0)
                return string.Empty;
            if (prefixLen <= 0 || prefixLen >= words.Length)
                return string.Join(" ", words);
            return string.Join(" ", words, prefixLen, words.Length - prefixLen);
        }
    }
}
