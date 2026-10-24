using System;
using System.Collections.Generic;
using ZXMAK2.Host.WinForms.Lib.Layout;

namespace ZXMAK2.Host.WinForms.Lib
{
    /// <summary>
    /// One-line selector. The closed bar shows the current item; opening it
    /// presents the choices in a drop-down drawn by the presenter.
    /// </summary>
    public class ComboBox : KozuiControl
    {
        private readonly List<string> _items = new List<string>();
        private int _selectedIndex = -1;
        private int _highlightIndex = -1;
        private int _dropDownScroll;
        private bool _isDropDownOpen;

        public event EventHandler SelectedIndexChanged;

        public ComboBox()
        {
            MinWidth = 12;
            MinHeight = 1;
            VerticalAlignment = VerticalAlignment.Top;
        }

        public int ItemCount => _items.Count;

        public int DropDownScroll => _dropDownScroll;

        public string GetItem(int index)
        {
            if (index < 0 || index >= _items.Count)
                return string.Empty;
            return _items[index] ?? string.Empty;
        }

        public string SelectedText
        {
            get
            {
                if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
                    return string.Empty;
                return _items[_selectedIndex] ?? string.Empty;
            }
        }

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (_items.Count == 0)
                    value = -1;
                else if (value < 0)
                    value = 0;
                else if (value >= _items.Count)
                    value = _items.Count - 1;

                if (!SetProperty(ref _selectedIndex, value))
                    return;

                _highlightIndex = value;
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public int HighlightIndex => _highlightIndex;

        public bool IsDropDownOpen
        {
            get => _isDropDownOpen;
            private set => SetProperty(ref _isDropDownOpen, value);
        }

        /// <summary>
        /// Text drawn before the selected item on the closed bar.
        /// Empty shows the item alone. File types use "Type: ".
        /// </summary>
        public string ClosedPrefix { get; set; }

        public void SetItems(IEnumerable<string> items)
        {
            _items.Clear();
            if (items != null)
            {
                foreach (var item in items)
                    _items.Add(item ?? string.Empty);
            }

            _dropDownScroll = 0;
            _isDropDownOpen = false;
            _selectedIndex = _items.Count > 0 ? 0 : -1;
            _highlightIndex = _selectedIndex;
            OnPropertyChanged(nameof(SelectedIndex));
        }

        public void OpenDropDown()
        {
            if (_items.Count == 0 || _isDropDownOpen)
                return;
            _highlightIndex = _selectedIndex >= 0 ? _selectedIndex : 0;
            IsDropDownOpen = true;
        }

        public void CloseDropDown()
        {
            if (!_isDropDownOpen)
                return;
            IsDropDownOpen = false;
        }

        public void ToggleDropDown()
        {
            if (_isDropDownOpen)
                CloseDropDown();
            else
                OpenDropDown();
        }

        public void MoveHighlight(int delta)
        {
            if (_items.Count == 0 || delta == 0)
                return;
            var next = _highlightIndex < 0 ? 0 : _highlightIndex + delta;
            SetHighlight(next);
        }

        public void SetHighlight(int index)
        {
            if (_items.Count == 0)
                return;
            if (index < 0)
                index = 0;
            else if (index >= _items.Count)
                index = _items.Count - 1;
            if (_highlightIndex == index)
                return;
            _highlightIndex = index;
            OnPropertyChanged(nameof(HighlightIndex));
        }

        public void CommitHighlight()
        {
            var index = _highlightIndex;
            CloseDropDown();
            if (index >= 0 && index < _items.Count)
                SelectedIndex = index;
        }

        public void EnsureHighlightVisible(int visibleRows)
        {
            if (visibleRows < 1)
                visibleRows = 1;
            if (_items.Count == 0)
            {
                _dropDownScroll = 0;
                return;
            }

            var index = _highlightIndex < 0 ? 0 : _highlightIndex;
            if (index < _dropDownScroll)
                _dropDownScroll = index;
            else if (index >= _dropDownScroll + visibleRows)
                _dropDownScroll = index - visibleRows + 1;

            var max = Math.Max(0, _items.Count - visibleRows);
            if (_dropDownScroll < 0)
                _dropDownScroll = 0;
            else if (_dropDownScroll > max)
                _dropDownScroll = max;
        }
    }
}
