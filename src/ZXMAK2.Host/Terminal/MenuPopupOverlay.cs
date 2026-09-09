using System;
using System.Collections.Generic;
using System.Linq;
using Kozynax.UI;

namespace ZXMAK2.Host.Terminal
{
    /// <summary>
    /// Cascading vertical menu popups shared by the main menu bar and context menus.
    /// </summary>
    public sealed class MenuPopupOverlay
    {
        private static readonly TerminalColor PopupBg = TerminalColor.Rgb(22, 26, 38);
        private static readonly TerminalColor Border = TerminalColor.Rgb(55, 65, 90);
        private static readonly TerminalColor Fg = TerminalColor.Rgb(230, 230, 230);
        private static readonly TerminalColor Disabled = TerminalColor.Rgb(90, 95, 110);
        private static readonly TerminalColor SelectedBg = TerminalColor.Rgb(30, 100, 210);
        private static readonly TerminalColor Accent = TerminalColor.Rgb(240, 220, 120);
        private static readonly TerminalColor Separator = TerminalColor.Rgb(70, 78, 100);

        private readonly ITerminal _terminal;
        private readonly object _commandParameter;
        private readonly int _scale;
        private readonly List<Popup> _popups = new List<Popup>();

        private int _pad;
        private int _lineH;

        public MenuPopupOverlay(ITerminal terminal, object commandParameter = null, int scale = 1)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            _commandParameter = commandParameter;
            _scale = Math.Max(1, scale);
            RebuildMetrics();
        }

        /// <summary>Minimum Y used when clamping popup position (e.g. below a menu bar).</summary>
        public int ClampMinY { get; set; }

        public bool IsOpen => _popups.Count > 0;

        public int Depth => _popups.Count;

        public void Clear()
            => _popups.Clear();

        public void OpenRoot(IReadOnlyList<MenuNode> items, int x, int y)
        {
            _popups.Clear();
            if (items == null || items.Count == 0)
                return;

            RebuildMetrics();
            var popup = BuildPopup(items, x, y);
            popup.SelectedIndex = FirstSelectableIndex(popup);
            _popups.Add(popup);
        }

        public void Draw()
        {
            RebuildMetrics();
            foreach (var popup in _popups)
                DrawPopup(popup);
        }

        public bool ContainsPoint(int x, int y)
            => HitPopup(x, y, out _, out _);

        public bool HitTest(int x, int y, out int popupIndex, out int itemIndex)
            => HitPopup(x, y, out popupIndex, out itemIndex);

        /// <summary>
        /// Keyboard / mouse handling while one or more popups are open.
        /// Returns a close reason when the overlay (or host) should dismiss;
        /// <c>null</c> when the event was handled without closing, or ignored.
        /// Left-at-root and outside-click are reported as <see cref="MenuBarCloseReason.Dismissed"/>
        /// so the host can decide whether to close only the overlay or the whole chrome.
        /// </summary>
        public MenuBarCloseReason? ProcessEvent(TerminalEvent ev)
        {
            if (!IsOpen)
                return null;

            switch (ev.Kind)
            {
                case TerminalEventKind.Quit:
                    return MenuBarCloseReason.Quit;

                case TerminalEventKind.KeyDown:
                    return HandleKey(ev.Key);

                case TerminalEventKind.MouseMove:
                    HandleMouseMove(ev.X, ev.Y);
                    return null;

                case TerminalEventKind.MouseDown:
                    if (ev.Button == TerminalMouseButton.Right)
                        return MenuBarCloseReason.Dismissed;
                    if (ev.Button == TerminalMouseButton.Left)
                    {
                        if (HitPopup(ev.X, ev.Y, out _, out _))
                            return null;
                        return MenuBarCloseReason.Dismissed;
                    }
                    return null;

                case TerminalEventKind.MouseUp:
                    if (ev.Button == TerminalMouseButton.Left)
                        return HandleMouseUp(ev.X, ev.Y);
                    return null;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Host-driven: select an item (e.g. after a mouse hover that MenuBar also tracks).
        /// Opens a child cascade when the item has children.
        /// </summary>
        public void SelectItem(int popupIndex, int itemIndex)
        {
            if (popupIndex < 0 || popupIndex >= _popups.Count)
                return;
            var popup = _popups[popupIndex];
            if (itemIndex < 0 || itemIndex >= popup.Items.Count)
                return;
            if (IsSeparator(popup.Items[itemIndex]))
                return;

            popup.SelectedIndex = itemIndex;
            Truncate(popupIndex + 1);

            var node = popup.Items[itemIndex];
            if (node.HasChildren)
                OpenChildPopup(popup, node);
        }

        public MenuBarCloseReason? ActivateAt(int popupIndex, int itemIndex)
        {
            SelectItem(popupIndex, itemIndex);
            if (popupIndex < 0 || popupIndex >= _popups.Count)
                return null;
            var popup = _popups[popupIndex];
            if (itemIndex < 0 || itemIndex >= popup.Items.Count)
                return null;
            var node = popup.Items[itemIndex];
            if (node.HasChildren)
                return null;
            return ActivateNode(node);
        }

        /// <summary>True when a child cascade was opened for the current selection.</summary>
        public bool TryOpenSelectedChild()
        {
            var deepest = Deepest();
            var node = SelectedNode(deepest);
            if (node == null || !node.HasChildren)
                return false;
            OpenChildPopup(deepest, node);
            return true;
        }

        private MenuBarCloseReason? HandleKey(TerminalKey key)
        {
            if (key == TerminalKey.Escape)
            {
                _popups.RemoveAt(_popups.Count - 1);
                return _popups.Count == 0 ? MenuBarCloseReason.Dismissed : (MenuBarCloseReason?)null;
            }

            if (key == TerminalKey.Left)
            {
                // Nested only — root Left is a no-op (menu bar switches tops itself).
                if (_popups.Count > 1)
                    _popups.RemoveAt(_popups.Count - 1);
                return null;
            }

            if (key == TerminalKey.Right)
            {
                TryOpenSelectedChild();
                return null;
            }

            if (key == TerminalKey.Up)
            {
                MovePopupSel(-1);
                return null;
            }

            if (key == TerminalKey.Down)
            {
                MovePopupSel(1);
                return null;
            }

            if (key == TerminalKey.Enter)
                return ActivateSelected();

            return null;
        }

        private MenuBarCloseReason? HandleMouseUp(int x, int y)
        {
            if (!HitPopup(x, y, out var popupIndex, out var itemIndex))
                return null;
            return ActivateAt(popupIndex, itemIndex);
        }

        private void HandleMouseMove(int x, int y)
        {
            if (!HitPopup(x, y, out var popupIndex, out var itemIndex))
                return;
            SelectItem(popupIndex, itemIndex);
        }

        private MenuBarCloseReason? ActivateSelected()
        {
            var deepest = Deepest();
            var node = SelectedNode(deepest);
            if (node == null)
                return null;

            if (node.HasChildren)
            {
                OpenChildPopup(deepest, node);
                return null;
            }

            return ActivateNode(node);
        }

        private MenuBarCloseReason? ActivateNode(MenuNode node)
        {
            if (node == null || IsSeparator(node) || node.Command == null)
                return null;

            var param = ResolveParameter(node);
            if (!node.Command.CanExecute(param))
                return null;

            node.Command.Execute(param);

            if (IsToggleCommand(node))
                return null;

            return MenuBarCloseReason.CommandExecuted;
        }

        private void OpenChildPopup(Popup parent, MenuNode node)
        {
            if (node == null || !node.HasChildren)
                return;

            var parentIndex = _popups.IndexOf(parent);
            if (parentIndex < 0)
                return;

            if (_popups.Count > parentIndex + 1
                && ReferenceEquals(_popups[parentIndex + 1].Source, node))
                return;

            Truncate(parentIndex + 1);

            var rowY = parent.Y + parent.SelectedIndex * _lineH;
            var popup = BuildPopup(node.Children, parent.X + parent.Width - _scale, rowY);
            popup.Source = node;
            popup.SelectedIndex = FirstSelectableIndex(popup);
            _popups.Add(popup);
        }

        private Popup BuildPopup(IReadOnlyList<MenuNode> items, int preferredX, int preferredY)
        {
            var filtered = items.Where(i => i != null).ToList();
            var maxText = 0;
            foreach (var item in filtered)
                maxText = Math.Max(maxText, _terminal.MeasureTextWidth(FormatItem(item), _scale));

            var width = Math.Max(maxText + _pad * 2, TerminalFont.GlyphWidth * _scale * 8);
            var height = Math.Max(filtered.Count, 1) * _lineH + _pad;
            var winW = Math.Max(_terminal.Width, 1);
            var winH = Math.Max(_terminal.Height, 1);
            var minY = Math.Max(0, ClampMinY);

            var x = preferredX;
            var y = preferredY;
            if (x + width > winW)
                x = Math.Max(0, winW - width);
            if (y + height > winH)
                y = Math.Max(minY, winH - height);
            if (x < 0)
                x = 0;
            if (y < minY)
                y = minY;

            return new Popup
            {
                Items = filtered,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                SelectedIndex = 0,
            };
        }

        private void Truncate(int keepCount)
        {
            while (_popups.Count > keepCount)
                _popups.RemoveAt(_popups.Count - 1);
        }

        private void MovePopupSel(int delta)
        {
            var deepest = Deepest();
            if (deepest == null || deepest.Items.Count == 0)
                return;

            var index = deepest.SelectedIndex;
            for (var step = 0; step < deepest.Items.Count; step++)
            {
                index = (index + delta + deepest.Items.Count) % deepest.Items.Count;
                if (!IsSeparator(deepest.Items[index]))
                    break;
            }

            deepest.SelectedIndex = index;
            var deepestIndex = _popups.Count - 1;
            Truncate(deepestIndex + 1);
            var node = SelectedNode(deepest);
            if (node != null && node.HasChildren)
                OpenChildPopup(deepest, node);
        }

        private Popup Deepest()
            => _popups.Count > 0 ? _popups[_popups.Count - 1] : null;

        private static MenuNode SelectedNode(Popup popup)
        {
            if (popup == null || popup.SelectedIndex < 0 || popup.SelectedIndex >= popup.Items.Count)
                return null;
            return popup.Items[popup.SelectedIndex];
        }

        private static int FirstSelectableIndex(Popup popup)
        {
            if (popup == null)
                return 0;
            for (var i = 0; i < popup.Items.Count; i++)
            {
                if (!IsSeparator(popup.Items[i]))
                    return i;
            }
            return 0;
        }

        private bool HitPopup(int x, int y, out int popupIndex, out int itemIndex)
        {
            popupIndex = -1;
            itemIndex = -1;
            for (var i = _popups.Count - 1; i >= 0; i--)
            {
                var p = _popups[i];
                if (x < p.X || x >= p.X + p.Width || y < p.Y || y >= p.Y + p.Height)
                    continue;

                var row = (y - p.Y - _pad / 2) / _lineH;
                if (row < 0 || row >= p.Items.Count)
                    return false;

                popupIndex = i;
                itemIndex = row;
                return true;
            }
            return false;
        }

        private void RebuildMetrics()
        {
            _pad = 4 * _scale;
            _lineH = TerminalFont.GlyphHeight * _scale + 2 * _scale;
        }

        private void DrawPopup(Popup popup)
        {
            _terminal.FillRect(popup.X, popup.Y, popup.Width, popup.Height, PopupBg);
            DrawBorder(popup.X, popup.Y, popup.Width, popup.Height);

            for (var i = 0; i < popup.Items.Count; i++)
            {
                var item = popup.Items[i];
                var rowY = popup.Y + _pad / 2 + i * _lineH;

                if (IsSeparator(item))
                {
                    var mid = rowY + _lineH / 2;
                    _terminal.FillRect(
                        popup.X + _pad,
                        mid,
                        Math.Max(0, popup.Width - _pad * 2),
                        Math.Max(1, _scale),
                        Separator);
                    continue;
                }

                if (i == popup.SelectedIndex)
                    _terminal.FillRect(popup.X + _scale, rowY, popup.Width - 2 * _scale, _lineH, SelectedBg);

                var enabled = IsEnabled(item);
                var color = !enabled ? Disabled : (i == popup.SelectedIndex ? Accent : Fg);
                var text = FormatItem(item);
                _terminal.DrawText(
                    popup.X + _pad,
                    rowY + (_lineH - TerminalFont.GlyphHeight * _scale) / 2,
                    text,
                    _scale,
                    color);
            }
        }

        private void DrawBorder(int x, int y, int w, int h)
        {
            var t = Math.Max(1, _scale);
            _terminal.FillRect(x, y, w, t, Border);
            _terminal.FillRect(x, y + h - t, w, t, Border);
            _terminal.FillRect(x, y, t, h, Border);
            _terminal.FillRect(x + w - t, y, t, h, Border);
        }

        private bool IsEnabled(MenuNode node)
        {
            if (node == null || IsSeparator(node))
                return false;
            if (node.Command == null)
                return node.HasChildren;
            return node.Command.CanExecute(ResolveParameter(node.Parameter, node.Command, node.IsChecked != null));
        }

        private object ResolveParameter(MenuNode node)
            => ResolveParameter(node.Parameter, node.Command, node.IsChecked != null);

        private object ResolveParameter(object parameter, ZXMAK2.Mvvm.ICommand command, bool isToggle)
        {
            if (parameter != null)
                return parameter;
            if (isToggle || (command != null && command.CanExecute(null)))
                return null;
            return _commandParameter;
        }

        internal static bool IsSeparator(MenuNode node)
            => node != null
               && node.Command == null
               && !node.HasChildren
               && (node.Caption == "-" || node.Caption == "—");

        internal static bool IsToggleCommand(MenuNode node)
            => node?.IsChecked != null;

        internal static string FormatItem(MenuNode node)
        {
            if (node == null)
                return string.Empty;
            if (IsSeparator(node))
                return string.Empty;

            var text = node.Command != null
                ? (node.Command.Text ?? node.Caption ?? string.Empty)
                : (node.Caption ?? string.Empty);

            string prefix;
            if (node.IsChecked != null)
                prefix = node.IsChecked() ? "[x] " : "[ ] ";
            else
                prefix = "    ";

            var suffix = node.HasChildren ? " >" : string.Empty;
            return prefix + text + suffix;
        }

        /// <summary>Factory helper for separator rows (Caption = "-").</summary>
        public static MenuNode SeparatorNode()
            => new MenuNode { Caption = "-" };

        private sealed class Popup
        {
            public MenuNode Source;
            public List<MenuNode> Items;
            public int X;
            public int Y;
            public int Width;
            public int Height;
            public int SelectedIndex;
        }
    }
}
