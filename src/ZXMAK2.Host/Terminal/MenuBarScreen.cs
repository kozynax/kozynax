using System;
using System.Collections.Generic;
using System.Linq;
using Kozynax.UI;

namespace ZXMAK2.Host.Terminal
{
    public enum MenuBarCloseReason
    {
        /// <summary>F9, Esc at root, click outside, or quit.</summary>
        Dismissed,
        /// <summary>A non-toggle command executed (caller may reopen after nested dialogs).</summary>
        CommandExecuted,
        Quit,
    }

    /// <summary>
    /// Classic horizontal menu bar overlay: top row + vertical dropdowns + cascading submenus.
    /// Draws through <see cref="ITerminal"/> only (SDL / other hosts).
    /// </summary>
    public sealed class MenuBarScreen
    {
        private static readonly TerminalColor BarBg = TerminalColor.Rgb(28, 32, 48);
        private static readonly TerminalColor PopupBg = TerminalColor.Rgb(22, 26, 38);
        private static readonly TerminalColor Border = TerminalColor.Rgb(55, 65, 90);
        private static readonly TerminalColor Fg = TerminalColor.Rgb(230, 230, 230);
        private static readonly TerminalColor Disabled = TerminalColor.Rgb(90, 95, 110);
        private static readonly TerminalColor SelectedBg = TerminalColor.Rgb(30, 100, 210);
        private static readonly TerminalColor Accent = TerminalColor.Rgb(240, 220, 120);

        private readonly ITerminal _terminal;
        private readonly object _commandParameter;
        private readonly int _scale;

        private MenuNode _root;
        private readonly List<TopItem> _tops = new List<TopItem>();
        private readonly List<Popup> _popups = new List<Popup>();
        private int _topIndex;
        private bool _open;
        private int _barHeight;
        private int _pad;
        private int _lineH;
        private int _lastWidth;
        private int _lastHeight;

        /// <summary>
        /// Optional live background drawn before the menu chrome (e.g. emulator frame).
        /// When set, <see cref="ITerminal.Clear"/> is skipped so the underlay stays visible.
        /// </summary>
        public Action Underlay { get; set; }

        public MenuBarScreen(ITerminal terminal, object commandParameter = null, int scale = 1)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            _commandParameter = commandParameter;
            _scale = Math.Max(1, scale);
        }

        /// <summary>
        /// True when closed via F9 / Esc / outside click (not a command).
        /// </summary>
        public bool ClosedByUser { get; private set; }

        public MenuBarCloseReason Run(MenuNode root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (!_terminal.IsAvailable)
                return MenuBarCloseReason.Dismissed;

            _root = root;
            ClosedByUser = false;
            _open = false;
            _popups.Clear();
            _topIndex = 0;
            RebuildMetrics();
            LayoutTops();

            while (true)
            {
                while (_terminal.PollEvent(out var ev))
                {
                    var reason = HandleEvent(ev);
                    if (reason.HasValue)
                        return reason.Value;
                }

                Draw();
                TerminalUiSession.AfterFrame(_terminal);
            }
        }

        private MenuBarCloseReason? HandleEvent(TerminalEvent ev)
        {
            switch (ev.Kind)
            {
                case TerminalEventKind.Quit:
                    ClosedByUser = true;
                    return MenuBarCloseReason.Quit;

                case TerminalEventKind.KeyDown:
                    return HandleKey(ev.Key);

                case TerminalEventKind.MouseMove:
                    HandleMouseMove(ev.X, ev.Y);
                    return null;

                case TerminalEventKind.MouseDown:
                    if (ev.Button == TerminalMouseButton.Right)
                    {
                        ClosedByUser = true;
                        return MenuBarCloseReason.Dismissed;
                    }
                    if (ev.Button == TerminalMouseButton.Left)
                        return HandleMouseDown(ev.X, ev.Y);
                    return null;

                case TerminalEventKind.MouseUp:
                    if (ev.Button == TerminalMouseButton.Left)
                        return HandleMouseUp(ev.X, ev.Y);
                    return null;

                default:
                    return null;
            }
        }

        private MenuBarCloseReason? HandleKey(TerminalKey key)
        {
            if (key == TerminalKey.F9)
            {
                ClosedByUser = true;
                return MenuBarCloseReason.Dismissed;
            }

            if (key == TerminalKey.Escape)
            {
                if (_popups.Count > 0)
                {
                    _popups.RemoveAt(_popups.Count - 1);
                    if (_popups.Count == 0)
                        _open = false;
                    return null;
                }

                ClosedByUser = true;
                return MenuBarCloseReason.Dismissed;
            }

            if (!_open)
            {
                if (key == TerminalKey.Left)
                {
                    MoveTop(-1);
                    return null;
                }
                if (key == TerminalKey.Right)
                {
                    MoveTop(1);
                    return null;
                }
                if (key == TerminalKey.Down || key == TerminalKey.Enter)
                {
                    OpenTop(_topIndex);
                    return null;
                }
                return null;
            }

            if (key == TerminalKey.Left)
            {
                if (_popups.Count > 1)
                {
                    _popups.RemoveAt(_popups.Count - 1);
                    return null;
                }
                MoveTop(-1);
                OpenTop(_topIndex);
                return null;
            }

            if (key == TerminalKey.Right)
            {
                var deepest = Deepest();
                if (deepest != null)
                {
                    var node = SelectedNode(deepest);
                    if (node != null && node.HasChildren)
                    {
                        OpenChildPopup(deepest, node);
                        return null;
                    }
                }
                MoveTop(1);
                OpenTop(_topIndex);
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

        private MenuBarCloseReason? HandleMouseDown(int x, int y)
        {
            var top = HitTop(x, y);
            if (top >= 0)
            {
                if (_open && _topIndex == top)
                {
                    ClosePopups();
                    return null;
                }
                OpenTop(top);
                return null;
            }

            if (_open && HitPopup(x, y, out _, out _))
                return null;

            ClosedByUser = true;
            return MenuBarCloseReason.Dismissed;
        }

        private MenuBarCloseReason? HandleMouseUp(int x, int y)
        {
            if (!_open)
                return null;

            if (!HitPopup(x, y, out var popupIndex, out var itemIndex))
                return null;

            var popup = _popups[popupIndex];
            popup.SelectedIndex = itemIndex;
            TruncatePopups(popupIndex + 1);
            var node = popup.Items[itemIndex];
            if (node.HasChildren)
            {
                OpenChildPopup(popup, node);
                return null;
            }

            return ActivateNode(node);
        }

        private void HandleMouseMove(int x, int y)
        {
            var top = HitTop(x, y);
            if (top >= 0)
            {
                _topIndex = top;
                if (_open)
                    OpenTop(top);
                return;
            }

            if (!_open)
                return;

            if (!HitPopup(x, y, out var popupIndex, out var itemIndex))
                return;

            var popup = _popups[popupIndex];
            if (popup.SelectedIndex != itemIndex)
            {
                popup.SelectedIndex = itemIndex;
                TruncatePopups(popupIndex + 1);
            }

            var node = popup.Items[itemIndex];
            if (node.HasChildren)
                OpenChildPopup(popup, node);
            else
                TruncatePopups(popupIndex + 1);
        }

        private MenuBarCloseReason? ActivateSelected()
        {
            if (!_open || _popups.Count == 0)
            {
                OpenTop(_topIndex);
                return null;
            }

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
            if (node?.Command == null)
                return null;

            var param = node.Parameter ?? _commandParameter;
            if (!node.Command.CanExecute(param))
                return null;

            node.Command.Execute(param);

            if (IsToggleCommand(node))
                return null;

            ClosedByUser = false;
            return MenuBarCloseReason.CommandExecuted;
        }

        private void OpenTop(int index)
        {
            if (index < 0 || index >= _tops.Count)
                return;

            _topIndex = index;
            _open = true;
            _popups.Clear();

            var top = _tops[index];
            var items = top.Node.Children ?? new List<MenuNode>();
            if (items.Count == 0)
                return;

            var popup = BuildPopup(items, top.X, _barHeight);
            popup.SelectedIndex = 0;
            _popups.Add(popup);
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

            TruncatePopups(parentIndex + 1);

            var rowY = parent.Y + parent.SelectedIndex * _lineH;
            var popup = BuildPopup(node.Children, parent.X + parent.Width - _scale, rowY);
            popup.Source = node;
            popup.SelectedIndex = 0;
            _popups.Add(popup);
        }

        private Popup BuildPopup(List<MenuNode> items, int preferredX, int preferredY)
        {
            var filtered = items.Where(i => i != null).ToList();
            var maxText = 0;
            foreach (var item in filtered)
                maxText = Math.Max(maxText, _terminal.MeasureTextWidth(FormatItem(item), _scale));

            var width = Math.Max(maxText + _pad * 2, TerminalFont.GlyphWidth * _scale * 8);
            var height = Math.Max(filtered.Count, 1) * _lineH + _pad;
            var winW = Math.Max(_terminal.Width, 1);
            var winH = Math.Max(_terminal.Height, 1);

            var x = preferredX;
            var y = preferredY;
            if (x + width > winW)
                x = Math.Max(0, winW - width);
            if (y + height > winH)
                y = Math.Max(_barHeight, winH - height);
            if (x < 0)
                x = 0;
            if (y < _barHeight)
                y = _barHeight;

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

        private void ClosePopups()
        {
            _popups.Clear();
            _open = false;
        }

        private void TruncatePopups(int keepCount)
        {
            while (_popups.Count > keepCount)
                _popups.RemoveAt(_popups.Count - 1);
            if (_popups.Count == 0)
                _open = false;
        }

        private void SyncCascadeFromHover()
        {
            if (_popups.Count == 0)
                return;
            var deepest = Deepest();
            var node = SelectedNode(deepest);
            if (node != null && node.HasChildren)
                OpenChildPopup(deepest, node);
            else
                TruncatePopups(_popups.Count);
        }

        private void MoveTop(int delta)
        {
            if (_tops.Count == 0)
                return;
            _topIndex = (_topIndex + delta + _tops.Count) % _tops.Count;
        }

        private void MovePopupSel(int delta)
        {
            var deepest = Deepest();
            if (deepest == null || deepest.Items.Count == 0)
                return;
            deepest.SelectedIndex = (deepest.SelectedIndex + delta + deepest.Items.Count) % deepest.Items.Count;
            // Drop any cascade that belonged to the previous row.
            var deepestIndex = _popups.Count - 1;
            TruncatePopups(deepestIndex + 1);
            SyncCascadeFromHover();
        }

        private Popup Deepest()
            => _popups.Count > 0 ? _popups[_popups.Count - 1] : null;

        private static MenuNode SelectedNode(Popup popup)
        {
            if (popup == null || popup.SelectedIndex < 0 || popup.SelectedIndex >= popup.Items.Count)
                return null;
            return popup.Items[popup.SelectedIndex];
        }

        private int HitTop(int x, int y)
        {
            if (y < 0 || y >= _barHeight)
                return -1;
            for (var i = 0; i < _tops.Count; i++)
            {
                var t = _tops[i];
                if (x >= t.X && x < t.X + t.Width)
                    return i;
            }
            return -1;
        }

        private bool HitPopup(int x, int y, out int popupIndex, out int itemIndex)
        {
            popupIndex = -1;
            itemIndex = -1;
            // Front-most first
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
            _barHeight = _lineH + _pad;
        }

        private void LayoutTops()
        {
            _tops.Clear();
            var children = _root.Children ?? new List<MenuNode>();
            var x = _pad;
            foreach (var child in children.Where(c => c != null))
            {
                var text = child.Caption ?? string.Empty;
                var tw = _terminal.MeasureTextWidth(text, _scale);
                var width = tw + _pad * 2;
                _tops.Add(new TopItem
                {
                    Node = child,
                    Text = text,
                    X = x,
                    Width = width,
                });
                x += width + _pad;
            }
            if (_topIndex >= _tops.Count)
                _topIndex = Math.Max(0, _tops.Count - 1);
        }

        private void Draw()
        {
            RebuildMetrics();
            var winW = _terminal.Width;
            var winH = _terminal.Height;
            if (winW != _lastWidth || winH != _lastHeight)
            {
                _lastWidth = winW;
                _lastHeight = winH;
                var oldTop = _topIndex;
                LayoutTops();
                _topIndex = oldTop < _tops.Count ? oldTop : Math.Max(0, _tops.Count - 1);
                if (_open && _topIndex >= 0 && _topIndex < _tops.Count)
                    OpenTop(_topIndex);
            }

            if (Underlay != null)
                Underlay();
            else
                _terminal.Clear(TerminalColor.Rgb(16, 18, 28));

            // Menu bar
            _terminal.FillRect(0, 0, winW, _barHeight, BarBg);
            _terminal.FillRect(0, _barHeight - _scale, winW, _scale, Border);

            for (var i = 0; i < _tops.Count; i++)
            {
                var t = _tops[i];
                if (i == _topIndex)
                    _terminal.FillRect(t.X, 0, t.Width, _barHeight, SelectedBg);
                var color = i == _topIndex ? Accent : Fg;
                _terminal.DrawText(t.X + _pad, (_barHeight - TerminalFont.GlyphHeight * _scale) / 2, t.Text, _scale, color);
            }

            foreach (var popup in _popups)
                DrawPopup(popup);

            _terminal.Present();
        }

        private void DrawPopup(Popup popup)
        {
            _terminal.FillRect(popup.X, popup.Y, popup.Width, popup.Height, PopupBg);
            DrawBorder(popup.X, popup.Y, popup.Width, popup.Height);

            for (var i = 0; i < popup.Items.Count; i++)
            {
                var item = popup.Items[i];
                var rowY = popup.Y + _pad / 2 + i * _lineH;
                if (i == popup.SelectedIndex)
                    _terminal.FillRect(popup.X + _scale, rowY, popup.Width - 2 * _scale, _lineH, SelectedBg);

                var enabled = IsEnabled(item);
                var color = !enabled ? Disabled : (i == popup.SelectedIndex ? Accent : Fg);
                var text = FormatItem(item);
                _terminal.DrawText(popup.X + _pad, rowY + (_lineH - TerminalFont.GlyphHeight * _scale) / 2, text, _scale, color);
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
            if (node?.Command == null)
                return node != null && node.HasChildren;
            var param = node.Parameter ?? _commandParameter;
            return node.Command.CanExecute(param);
        }

        private static string FormatItem(MenuNode node)
        {
            if (node == null)
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

        internal static bool IsToggleCommand(MenuNode node)
        {
            if (node?.Command == null)
                return false;
            if (node.IsChecked != null)
                return true;
            var text = node.Command.Text ?? string.Empty;
            return text.IndexOf("Pause", StringComparison.OrdinalIgnoreCase) >= 0
                   || text.IndexOf("Resume", StringComparison.OrdinalIgnoreCase) >= 0
                   || text.IndexOf("Full Screen", StringComparison.OrdinalIgnoreCase) >= 0
                   || text.IndexOf("Windowed", StringComparison.OrdinalIgnoreCase) >= 0
                   || text.IndexOf("Maximum Speed", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private sealed class TopItem
        {
            public MenuNode Node;
            public string Text;
            public int X;
            public int Width;
        }

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
