using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ZXMAK2.Host.WinForms.Lib
{
    /// <summary>Character span within a list item string (no focus prefix).</summary>
    public readonly struct ListTextSpan
    {
        public ListTextSpan(int start, int length)
        {
            Start = start;
            Length = length;
        }

        public int Start { get; }
        public int Length { get; }
    }

    /// <summary>Non-generic surface for presenters (item text + selection).</summary>
    public abstract class ListView : KozuiControl
    {
        public event EventHandler ItemActivated;

        /// <summary>When true, releasing the mouse over a row activates it (selection still follows press/drag).</summary>
        public bool ActivateOnClick { get; set; }

        /// <summary>
        /// When true (default), a mouse-up on an already-selected row activates it
        /// (second single-click). Disable for panels that require a real double-click.
        /// </summary>
        public bool ActivateOnSecondClick { get; set; } = true;

        /// <summary>
        /// When true, the selected row is not painted with a full-width background
        /// (use <see cref="GetHighlightSpans"/> for partial highlights).
        /// </summary>
        public bool SuppressRowHighlight { get; set; }

        /// <summary>
        /// Optional per-row highlight spans in <see cref="GetItemText"/> coordinates.
        /// </summary>
        public Func<int, IReadOnlyList<ListTextSpan>> GetHighlightSpans { get; set; }

        public abstract int Count { get; }
        public abstract int SelectedIndex { get; set; }
        public abstract string GetItemText(int index);

        public void ActivateItem()
            => ItemActivated?.Invoke(this, EventArgs.Empty);
    }

    public class ListView<T> : ListView
    {
        public delegate void IndexChangeEventHandler(object sender, int index);

        public event IndexChangeEventHandler SelectedIndexChanged;

        private int _selectedIndex = -1;

        public override int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (!SetProperty(ref _selectedIndex, value))
                    return;
                SelectedIndexChanged?.Invoke(this, _selectedIndex);
            }
        }

        public BindingList<T> List { get; } = new BindingList<T>();

        public Func<T, string> ItemTextSelector { get; set; }

        public override int Count => List.Count;

        public override string GetItemText(int index)
        {
            if (index < 0 || index >= List.Count)
                return string.Empty;
            var item = List[index];
            if (ItemTextSelector != null)
                return ItemTextSelector(item) ?? string.Empty;
            return item?.ToString() ?? string.Empty;
        }

        public void Reset(IEnumerable<T> items)
        {
            List.RaiseListChangedEvents = false;
            try
            {
                List.Clear();
                foreach (var item in items)
                    List.Add(item);
            }
            finally
            {
                List.RaiseListChangedEvents = true;
                List.ResetBindings();
            }
        }

        public void Reset(params T[] items)
            => Reset((IEnumerable<T>)items);

        public bool SequenceEqual(IEnumerable<T> other)
            => List.SequenceEqual(other);
    }
}
