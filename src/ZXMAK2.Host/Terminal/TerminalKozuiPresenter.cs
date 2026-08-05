using System;
using System.Collections.Generic;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Host.WinForms.Lib.Layout;
using ZXMAK2.Host.WinForms.Lib.Presenters;

namespace ZXMAK2.Host.Terminal
{
    /// <summary>
    /// Arranges a Kozui tree in character cells and paints controls via <see cref="ITerminal"/>.
    /// </summary>
    public sealed class TerminalKozuiPresenter : IKozuiPresenter
    {
        private const int PanelPadCells = 1;

        private static readonly TerminalColor Bg = TerminalColor.Rgb(16, 18, 28);
        private static readonly TerminalColor Fg = TerminalColor.Rgb(230, 230, 230);
        private static readonly TerminalColor Accent = TerminalColor.Rgb(240, 220, 120);
        private static readonly TerminalColor SelectedBg = TerminalColor.Rgb(30, 100, 210);
        private static readonly TerminalColor Disabled = TerminalColor.Rgb(90, 95, 110);
        private static readonly TerminalColor BarBg = TerminalColor.Rgb(40, 45, 60);
        private static readonly TerminalColor BarFg = TerminalColor.Rgb(80, 160, 220);
        private static readonly TerminalColor PanelBg = TerminalColor.Rgb(22, 26, 38);
        private static readonly TerminalColor PanelBgActive = TerminalColor.Rgb(36, 46, 68);
        private static readonly TerminalColor PanelBorder = TerminalColor.Rgb(55, 65, 90);

        private readonly ITerminal _terminal;
        private readonly int _scale;
        private KozuiControl _root;
        private readonly List<KozuiControl> _focusables = new List<KozuiControl>();
        private int _focusIndex;
        private int _listScroll;
        private KozuiControl _activeRegion;
        private bool _needsInitialFocus;

        public TerminalKozuiPresenter(ITerminal terminal, int scale = 1)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            _scale = Math.Max(1, scale);
        }

        public KozuiControl Root => _root;

        public void Attach(KozuiControl root)
        {
            _root = root;
            _listScroll = 0;
            _needsInitialFocus = true;
            _focusIndex = 0;
            RebuildFocusables();
        }

        public void MeasureArrange(LayoutSize availableCells)
        {
            if (_root == null)
                return;
            LayoutEngine.MeasureArrange(_root, availableCells);
        }

        public void MeasureArrangeFromTerminal()
        {
            MeasureArrange(GetAvailableCells());
        }

        public LayoutSize GetAvailableCells()
        {
            var cellW = TerminalFont.GlyphWidth * _scale;
            var cellH = TerminalFont.GlyphHeight * _scale;
            var cols = Math.Max(1, _terminal.Width / cellW);
            var rows = Math.Max(1, _terminal.Height / cellH);
            return new LayoutSize(cols, rows);
        }

        public void Render()
        {
            if (_root == null || !_terminal.IsAvailable)
                return;

            RebuildFocusables();
            _activeRegion = FindActiveRegion();
            _terminal.Clear(Bg);
            DrawControl(_root);
            _terminal.Present();
        }

        public bool RouteInput(KozuiInput input)
        {
            if (_root == null || input.Kind != KozuiInputKind.KeyDown)
                return false;

            RebuildFocusables();
            var focused = FocusedControl();

            if (focused is ListView listView && listView.Enabled)
            {
                if (HandleListInput(listView, input.Key))
                    return true;
            }

            if (focused is TrackBar trackBar && trackBar.Enabled)
            {
                if (HandleTrackBarInput(trackBar, input.Key))
                    return true;
            }

            switch (input.Key)
            {
                case KozuiInputKey.Tab:
                    MoveFocus(1);
                    return true;
                case KozuiInputKey.Right:
                    if (!(focused is ListView) && !(focused is TrackBar))
                    {
                        MoveFocus(1);
                        return true;
                    }
                    break;
                case KozuiInputKey.Left:
                    if (!(focused is ListView) && !(focused is TrackBar))
                    {
                        MoveFocus(-1);
                        return true;
                    }
                    break;
                case KozuiInputKey.Down:
                    if (!(focused is ListView))
                    {
                        MoveFocus(1);
                        return true;
                    }
                    break;
                case KozuiInputKey.Up:
                    if (!(focused is ListView))
                    {
                        MoveFocus(-1);
                        return true;
                    }
                    break;
                case KozuiInputKey.Enter:
                    ActivateFocused();
                    return true;
                case KozuiInputKey.Escape:
                    return false;
            }

            return false;
        }

        public static KozuiInputKey MapKey(TerminalKey key)
        {
            switch (key)
            {
                case TerminalKey.Escape: return KozuiInputKey.Escape;
                case TerminalKey.Enter: return KozuiInputKey.Enter;
                case TerminalKey.Tab: return KozuiInputKey.Tab;
                case TerminalKey.Left: return KozuiInputKey.Left;
                case TerminalKey.Right: return KozuiInputKey.Right;
                case TerminalKey.Up: return KozuiInputKey.Up;
                case TerminalKey.Down: return KozuiInputKey.Down;
                case TerminalKey.PageUp: return KozuiInputKey.PageUp;
                case TerminalKey.PageDown: return KozuiInputKey.PageDown;
                default: return KozuiInputKey.None;
            }
        }

        private static bool HandleTrackBarInput(TrackBar trackBar, KozuiInputKey key)
        {
            var step = Math.Max(1, (trackBar.Maximum - trackBar.Minimum) / 20);
            switch (key)
            {
                case KozuiInputKey.Left:
                    trackBar.Value = Math.Max(trackBar.Minimum, trackBar.Value - step);
                    return true;
                case KozuiInputKey.Right:
                    trackBar.Value = Math.Min(trackBar.Maximum, trackBar.Value + step);
                    return true;
                case KozuiInputKey.PageUp:
                    trackBar.Value = Math.Max(trackBar.Minimum, trackBar.Value - step * 4);
                    return true;
                case KozuiInputKey.PageDown:
                    trackBar.Value = Math.Min(trackBar.Maximum, trackBar.Value + step * 4);
                    return true;
                default:
                    return false;
            }
        }

        private bool HandleListInput(ListView listView, KozuiInputKey key)
        {
            var count = listView.Count;
            if (count <= 0)
                return false;

            var visible = ListContentRows(listView);
            switch (key)
            {
                case KozuiInputKey.Up:
                    listView.SelectedIndex = Math.Max(0, listView.SelectedIndex - 1);
                    EnsureListVisible(listView, visible);
                    return true;
                case KozuiInputKey.Down:
                    listView.SelectedIndex = Math.Min(count - 1, Math.Max(0, listView.SelectedIndex) + 1);
                    EnsureListVisible(listView, visible);
                    return true;
                case KozuiInputKey.PageUp:
                    listView.SelectedIndex = Math.Max(0, listView.SelectedIndex - visible);
                    EnsureListVisible(listView, visible);
                    return true;
                case KozuiInputKey.PageDown:
                    listView.SelectedIndex = Math.Min(count - 1, Math.Max(0, listView.SelectedIndex) + visible);
                    EnsureListVisible(listView, visible);
                    return true;
                default:
                    return false;
            }
        }

        private void EnsureListVisible(ListView listView, int visible)
        {
            var selected = listView.SelectedIndex;
            if (selected < _listScroll)
                _listScroll = selected;
            else if (selected >= _listScroll + visible)
                _listScroll = selected - visible + 1;
            if (_listScroll < 0)
                _listScroll = 0;
        }

        private void MoveFocus(int delta)
        {
            if (_focusables.Count == 0)
                return;
            _focusIndex = (_focusIndex + delta + _focusables.Count) % _focusables.Count;
            if (FocusedControl() is ListView list)
                EnsureListVisible(list, ListContentRows(list));
        }

        private void ActivateFocused()
        {
            var focused = FocusedControl();
            if (focused is Button button && button.Enabled)
            {
                button.Click(button, EventArgs.Empty);
                return;
            }

            if (focused is CheckBox checkBox && checkBox.Enabled)
            {
                checkBox.Checked = !checkBox.Checked;
                return;
            }

            if (focused is ListView && focused.Enabled)
            {
                // Double-click equivalent: play current block
                var play = FindButtonByText("Play") ?? FindButtonByText("Stop");
                if (play != null && play.Enabled)
                    play.Click(play, EventArgs.Empty);
            }
        }

        private Button FindButtonByText(string text)
        {
            foreach (var c in _focusables)
            {
                if (c is Button b && string.Equals(b.Text, text, StringComparison.Ordinal))
                    return b;
            }
            return FindButtonInTree(_root, text);
        }

        private static Button FindButtonInTree(KozuiControl control, string text)
        {
            if (control is Button button && string.Equals(button.Text, text, StringComparison.Ordinal))
                return button;
            if (control is Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    var found = FindButtonInTree(child, text);
                    if (found != null)
                        return found;
                }
            }
            else if (control is Placeholder placeholder && placeholder.Content != null)
            {
                return FindButtonInTree(placeholder.Content, text);
            }
            return null;
        }

        private KozuiControl FocusedControl()
        {
            if (_focusables.Count == 0 || _focusIndex < 0 || _focusIndex >= _focusables.Count)
                return null;
            return _focusables[_focusIndex];
        }

        private void RebuildFocusables()
        {
            var previous = FocusedControl();
            _focusables.Clear();
            if (_root != null)
                CollectFocusables(_root, _focusables);

            // Tab order follows on-screen position (top→bottom, then left→right),
            // not construction / dock-child order.
            _focusables.Sort(CompareVisualTabOrder);

            if (_needsInitialFocus)
            {
                // Before the first arrange, bounds are empty and tree order would
                // focus toolbar buttons. Wait until layout exists, then prefer a list.
                if (_focusables.Exists(c => c.ArrangedBounds.Width > 0 && c.ArrangedBounds.Height > 0))
                {
                    var listIndex = _focusables.FindIndex(c => c is ListView);
                    _focusIndex = listIndex >= 0 ? listIndex : 0;
                    _needsInitialFocus = false;
                }
                else
                {
                    _focusIndex = 0;
                }
                return;
            }

            if (previous != null)
            {
                var idx = _focusables.IndexOf(previous);
                if (idx >= 0)
                {
                    _focusIndex = idx;
                    return;
                }
            }

            if (_focusIndex >= _focusables.Count)
                _focusIndex = Math.Max(0, _focusables.Count - 1);
        }

        private static int CompareVisualTabOrder(KozuiControl a, KozuiControl b)
        {
            var ay = a.ArrangedBounds.Y;
            var by = b.ArrangedBounds.Y;
            if (ay != by)
                return ay.CompareTo(by);
            var ax = a.ArrangedBounds.X;
            var bx = b.ArrangedBounds.X;
            if (ax != bx)
                return ax.CompareTo(bx);
            return 0;
        }

        private static void CollectFocusables(KozuiControl control, List<KozuiControl> list)
        {
            if (control == null || !control.Visible)
                return;

            if (control is Button button && button.Enabled)
                list.Add(button);
            else if (control is CheckBox checkBox && checkBox.Enabled)
                list.Add(checkBox);
            else if (control is TrackBar trackBar && trackBar.Enabled)
                list.Add(trackBar);
            else if (control is ListView listView && listView.Enabled)
                list.Add(listView);

            if (control is Panel panel)
            {
                foreach (var child in panel.Children)
                    CollectFocusables(child, list);
            }
            else if (control is Placeholder placeholder && placeholder.Content != null)
            {
                CollectFocusables(placeholder.Content, list);
            }
        }

        private KozuiControl FindActiveRegion()
        {
            var focused = FocusedControl();
            if (focused == null)
                return null;

            if (focused is ListView)
                return focused;

            for (var c = focused; c != null; c = c.Parent)
            {
                if (c is Placeholder || c is ListView)
                    return c;
            }

            // Toolbar / button strip: highlight the nearest non-root panel.
            for (var c = focused.Parent; c != null; c = c.Parent)
            {
                if (c is Panel && !ReferenceEquals(c, _root))
                    return c;
            }

            return focused;
        }

        private bool IsActiveRegion(KozuiControl control)
            => control != null && ReferenceEquals(control, _activeRegion);

        private void DrawPanelChrome(LayoutRect bounds, bool active)
        {
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            var (px, py) = CellToPixel(bounds.X, bounds.Y);
            var pw = bounds.Width * TerminalFont.GlyphWidth * _scale;
            var ph = bounds.Height * TerminalFont.GlyphHeight * _scale;
            _terminal.FillRect(px, py, pw, ph, active ? PanelBgActive : PanelBg);
            if (!active)
                DrawRectOutline(px, py, pw, ph, PanelBorder, 1);
        }

        private static LayoutRect InsetPanelContent(LayoutRect bounds)
        {
            var pad = PanelPadCells;
            return new LayoutRect(
                bounds.X + pad,
                bounds.Y + pad,
                Math.Max(0, bounds.Width - pad * 2),
                Math.Max(0, bounds.Height - pad * 2));
        }

        private static int ListContentRows(ListView listView)
            => Math.Max(1, InsetPanelContent(listView.ArrangedBounds).Height);

        private void DrawRectOutline(int x, int y, int width, int height, TerminalColor color, int thickness)
        {
            thickness = Math.Max(1, thickness);
            if (width <= 0 || height <= 0)
                return;
            _terminal.FillRect(x, y, width, thickness, color);
            _terminal.FillRect(x, y + height - thickness, width, thickness, color);
            _terminal.FillRect(x, y, thickness, height, color);
            _terminal.FillRect(x + width - thickness, y, thickness, height, color);
        }

        private void DrawControl(KozuiControl control)
        {
            if (control == null || !control.Visible)
                return;

            if (control is Label label)
            {
                DrawLabel(label);
                return;
            }

            if (control is Button button)
            {
                DrawButton(button);
                return;
            }

            if (control is CheckBox checkBox)
            {
                DrawCheckBox(checkBox);
                return;
            }

            if (control is ProgressBar progressBar)
            {
                DrawProgressBar(progressBar);
                return;
            }

            if (control is TrackBar trackBar)
            {
                DrawTrackBar(trackBar);
                return;
            }

            if (control is ListView listView)
            {
                DrawPanelChrome(listView.ArrangedBounds, IsActiveRegion(listView));
                DrawListView(listView);
                return;
            }

            if (control is Placeholder placeholder)
            {
                DrawPanelChrome(placeholder.ArrangedBounds, IsActiveRegion(placeholder));
                if (placeholder.Content != null)
                    DrawControl(placeholder.Content);
                return;
            }

            if (control is Panel panel)
            {
                if (IsActiveRegion(panel))
                    DrawPanelChrome(panel.ArrangedBounds, active: true);
                foreach (var child in panel.Children)
                    DrawControl(child);
            }
        }

        private void DrawLabel(Label label)
        {
            var bounds = label.ArrangedBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;
            var text = Truncate(label.Text ?? string.Empty, bounds.Width);
            var (px, py) = CellToPixel(bounds.X, bounds.Y);
            _terminal.DrawText(px, py, text, _scale, label.Enabled ? Fg : Disabled);
        }

        private void DrawButton(Button button)
        {
            var bounds = button.ArrangedBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            var focused = ReferenceEquals(button, FocusedControl());
            var (px, py) = CellToPixel(bounds.X, bounds.Y);
            var pw = bounds.Width * TerminalFont.GlyphWidth * _scale;
            var ph = bounds.Height * TerminalFont.GlyphHeight * _scale;

            if (focused)
                _terminal.FillRect(px - 2, py - 1, pw + 4, ph + 2, SelectedBg);

            var label = button.Text ?? string.Empty;
            var framed = focused ? $"> {label} <" : $"[ {label} ]";
            framed = Truncate(framed, bounds.Width);
            var color = !button.Enabled ? Disabled : focused ? Accent : Fg;
            _terminal.DrawText(px, py, framed, _scale, color);
        }

        private void DrawCheckBox(CheckBox checkBox)
        {
            var bounds = checkBox.ArrangedBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            var focused = ReferenceEquals(checkBox, FocusedControl());
            var (px, py) = CellToPixel(bounds.X, bounds.Y);
            var pw = bounds.Width * TerminalFont.GlyphWidth * _scale;
            var ph = bounds.Height * TerminalFont.GlyphHeight * _scale;
            if (focused)
                _terminal.FillRect(px - 2, py - 1, pw + 4, ph + 2, SelectedBg);

            var mark = checkBox.Checked ? "x" : " ";
            var text = Truncate($"[{mark}] {checkBox.Text}", bounds.Width);
            var color = !checkBox.Enabled ? Disabled : focused ? Accent : Fg;
            _terminal.DrawText(px, py, text, _scale, color);
        }

        private void DrawProgressBar(ProgressBar bar)
        {
            var bounds = bar.ArrangedBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            var (px, py) = CellToPixel(bounds.X, bounds.Y);
            var pw = bounds.Width * TerminalFont.GlyphWidth * _scale;
            var ph = Math.Max(TerminalFont.GlyphHeight * _scale - 2, 4);
            _terminal.FillRect(px, py + 1, pw, ph, BarBg);

            var range = Math.Max(1, bar.Maximum - bar.Minimum);
            var value = Math.Max(bar.Minimum, Math.Min(bar.Maximum, bar.Value)) - bar.Minimum;
            var fill = (int)(pw * (value / (double)range));
            if (fill > 0)
                _terminal.FillRect(px, py + 1, fill, ph, BarFg);
        }

        private void DrawTrackBar(TrackBar bar)
        {
            var bounds = bar.ArrangedBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            var focused = ReferenceEquals(bar, FocusedControl());
            var (px, py) = CellToPixel(bounds.X, bounds.Y);
            var pw = bounds.Width * TerminalFont.GlyphWidth * _scale;
            var ph = Math.Max(TerminalFont.GlyphHeight * _scale - 2, 4);
            if (focused)
                _terminal.FillRect(px - 2, py - 1, pw + 4, ph + 2, SelectedBg);

            _terminal.FillRect(px, py + 1, pw, ph, BarBg);
            var range = Math.Max(1, bar.Maximum - bar.Minimum);
            var value = Math.Max(bar.Minimum, Math.Min(bar.Maximum, bar.Value)) - bar.Minimum;
            var fill = (int)(pw * (value / (double)range));
            if (fill > 0)
                _terminal.FillRect(px, py + 1, Math.Max(fill, 2), ph, focused ? Accent : BarFg);
        }

        private void DrawListView(ListView listView)
        {
            var bounds = listView.ArrangedBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            var content = InsetPanelContent(bounds);
            if (content.Width <= 0 || content.Height <= 0)
                return;

            var focused = ReferenceEquals(listView, FocusedControl());
            var visible = Math.Max(1, content.Height);
            EnsureListVisible(listView, visible);

            for (var row = 0; row < visible; row++)
            {
                var index = _listScroll + row;
                if (index >= listView.Count)
                    break;

                var y = content.Y + row;
                var (px, py) = CellToPixel(content.X, y);
                var pw = content.Width * TerminalFont.GlyphWidth * _scale;
                var ph = TerminalFont.GlyphHeight * _scale;
                var selected = index == listView.SelectedIndex;

                if (selected)
                    _terminal.FillRect(px, py, pw, ph, SelectedBg);

                var prefix = selected ? ">" : " ";
                var text = Truncate(prefix + listView.GetItemText(index), content.Width);
                var color = !listView.Enabled
                    ? Disabled
                    : selected
                        ? Accent
                        : Fg;
                _terminal.DrawText(px, py, text, _scale, color);
            }

            if (focused && listView.Count == 0)
            {
                var (px, py) = CellToPixel(content.X, content.Y);
                _terminal.DrawText(px, py, "(no blocks)", _scale, Disabled);
            }
        }

        private (int x, int y) CellToPixel(int cellX, int cellY)
            => (cellX * TerminalFont.GlyphWidth * _scale, cellY * TerminalFont.GlyphHeight * _scale);

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
    }
}
