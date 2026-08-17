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
        private static readonly TerminalColor PressedBg = TerminalColor.Rgb(18, 60, 140);
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
        private readonly Dictionary<ListView, int> _listScrollByView = new Dictionary<ListView, int>();
        private KozuiControl _activeRegion;
        private bool _needsInitialFocus;
        private Button _pressedButton;
        private bool _pressedHot;
        private ListView _pressedListView;
        private int _listPressIndex = -1;
        private bool _listPressWasSelected;

        public TerminalKozuiPresenter(ITerminal terminal, int scale = 1)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            _scale = Math.Max(1, scale);
        }

        /// <summary>Optional PNG blitter for <see cref="ImageView"/> controls.</summary>
        public IMenuImagePainter ImagePainter { get; set; }

        public KozuiControl Root => _root;

        /// <summary>Currently focused interactive control, if any.</summary>
        public KozuiControl Focused => FocusedControl();

        public void Focus(KozuiControl control)
        {
            RebuildFocusables();
            FocusControl(control);
            if (control is ListView list)
                EnsureListVisible(list, ListContentRows(list));
        }

        public void Attach(KozuiControl root)
        {
            _root = root;
            _listScrollByView.Clear();
            _needsInitialFocus = true;
            _focusIndex = 0;
            ClearPointerPress();
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
            if (_root == null)
                return false;

            RebuildFocusables();

            if (input.Kind == KozuiInputKind.MouseDown)
                return HandleMouseDown(input);
            if (input.Kind == KozuiInputKind.MouseMove)
                return HandleMouseMove(input);
            if (input.Kind == KozuiInputKind.MouseUp)
                return HandleMouseUp(input);
            if (input.Kind == KozuiInputKind.MouseWheel)
                return HandleMouseWheel(input);
            if (input.Kind != KozuiInputKind.KeyDown)
                return false;

            var focused = FocusedControl();

            if (focused is TextBox textBox && textBox.Enabled)
            {
                if (input.Char != '\0')
                {
                    textBox.InsertChar(input.Char);
                    return true;
                }
                if (input.Key == KozuiInputKey.Backspace)
                {
                    textBox.Backspace();
                    return true;
                }
            }

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
                    if (!(focused is ListView) && !(focused is TrackBar) && !(focused is TextBox))
                    {
                        MoveFocus(1);
                        return true;
                    }
                    break;
                case KozuiInputKey.Left:
                    if (!(focused is ListView) && !(focused is TrackBar) && !(focused is TextBox))
                    {
                        MoveFocus(-1);
                        return true;
                    }
                    break;
                case KozuiInputKey.Down:
                    if (!(focused is ListView) && !(focused is TextBox))
                    {
                        MoveFocus(1);
                        return true;
                    }
                    break;
                case KozuiInputKey.Up:
                    if (!(focused is ListView) && !(focused is TextBox))
                    {
                        MoveFocus(-1);
                        return true;
                    }
                    break;
                case KozuiInputKey.Enter:
                    if (focused is TextBox)
                        return false;
                    ActivateFocused();
                    return true;
                case KozuiInputKey.Escape:
                    return false;
            }

            return false;
        }

        /// <summary>
        /// Maps a terminal event to Kozui input when the presenter can handle it.
        /// Escape/Quit stay with the host view.
        /// </summary>
        public static bool TryMapEvent(TerminalEvent ev, out KozuiInput input)
        {
            input = default;
            switch (ev.Kind)
            {
                case TerminalEventKind.KeyDown:
                {
                    if (ev.Char != '\0' && !char.IsControl(ev.Char))
                    {
                        input = KozuiInput.TextInput(ev.Char);
                        return true;
                    }
                    var key = MapKey(ev.Key);
                    if (key == KozuiInputKey.None || key == KozuiInputKey.Escape)
                        return false;
                    input = KozuiInput.KeyDown(key);
                    return true;
                }
                case TerminalEventKind.MouseDown:
                    if (ev.Button != TerminalMouseButton.Left)
                        return false;
                    input = KozuiInput.MouseDown(ev.X, ev.Y, KozuiMouseButton.Left);
                    return true;
                case TerminalEventKind.MouseUp:
                    if (ev.Button != TerminalMouseButton.Left)
                        return false;
                    input = KozuiInput.MouseUp(ev.X, ev.Y, KozuiMouseButton.Left);
                    return true;
                case TerminalEventKind.MouseMove:
                    input = KozuiInput.MouseMove(ev.X, ev.Y);
                    return true;
                case TerminalEventKind.MouseWheel:
                    input = KozuiInput.MouseWheel(ev.X, ev.Y, ev.WheelDelta);
                    return true;
                default:
                    return false;
            }
        }

        public static KozuiInputKey MapKey(TerminalKey key)
        {
            switch (key)
            {
                case TerminalKey.Escape: return KozuiInputKey.Escape;
                case TerminalKey.Enter: return KozuiInputKey.Enter;
                case TerminalKey.Tab: return KozuiInputKey.Tab;
                case TerminalKey.Backspace: return KozuiInputKey.Backspace;
                case TerminalKey.Left: return KozuiInputKey.Left;
                case TerminalKey.Right: return KozuiInputKey.Right;
                case TerminalKey.Up: return KozuiInputKey.Up;
                case TerminalKey.Down: return KozuiInputKey.Down;
                case TerminalKey.PageUp: return KozuiInputKey.PageUp;
                case TerminalKey.PageDown: return KozuiInputKey.PageDown;
                default: return KozuiInputKey.None;
            }
        }

        private bool HandleMouseDown(KozuiInput input)
        {
            ClearPointerPress();

            PixelToCell(input.X, input.Y, out var cellX, out var cellY);
            var hit = HitTestInteractive(_root, cellX, cellY);
            if (hit == null || !hit.Enabled)
                return false;

            FocusControl(hit);

            if (hit is Button button)
            {
                _pressedButton = button;
                _pressedHot = true;
                return true;
            }

            if (hit is CheckBox checkBox)
            {
                checkBox.Checked = !checkBox.Checked;
                return true;
            }

            if (hit is TrackBar trackBar)
            {
                var bounds = trackBar.ArrangedBounds;
                if (bounds.Width > 0)
                {
                    var t = (cellX - bounds.X + 0.5) / bounds.Width;
                    t = Math.Max(0, Math.Min(1, t));
                    var range = trackBar.Maximum - trackBar.Minimum;
                    trackBar.Value = trackBar.Minimum + (int)Math.Round(t * range);
                }
                return true;
            }

            if (hit is ListView listView)
            {
                var index = HitListRow(listView, cellX, cellY);
                if (index >= 0)
                {
                    _pressedListView = listView;
                    _listPressIndex = index;
                    _listPressWasSelected = listView.SelectedIndex == index;
                    listView.SelectedIndex = index;
                }
                return true;
            }

            return true;
        }

        private bool HandleMouseMove(KozuiInput input)
        {
            PixelToCell(input.X, input.Y, out var cellX, out var cellY);

            if (_pressedListView != null)
            {
                if (_pressedListView.Enabled
                    && _pressedListView.Visible
                    && ContainsCell(_pressedListView.ArrangedBounds, cellX, cellY))
                {
                    var index = HitListRow(_pressedListView, cellX, cellY);
                    if (index >= 0)
                        _pressedListView.SelectedIndex = index;
                }
                return true;
            }

            if (_pressedButton == null)
                return false;

            _pressedHot = ContainsCell(_pressedButton.ArrangedBounds, cellX, cellY)
                          && _pressedButton.Enabled
                          && _pressedButton.Visible;
            return true;
        }

        private bool HandleMouseUp(KozuiInput input)
        {
            PixelToCell(input.X, input.Y, out var cellX, out var cellY);

            if (_pressedListView != null)
            {
                var list = _pressedListView;
                var pressIndex = _listPressIndex;
                var wasSelected = _listPressWasSelected;
                ClearPointerPress();

                if (!list.Enabled || !list.Visible)
                    return true;

                var index = HitListRow(list, cellX, cellY);
                if (index < 0)
                    return true;

                list.SelectedIndex = index;
                if (list.ActivateOnClick
                    || (list.ActivateOnSecondClick && wasSelected && index == pressIndex))
                    list.ActivateItem();
                return true;
            }

            if (_pressedButton == null)
                return false;

            var button = _pressedButton;
            var releaseInside = ContainsCell(button.ArrangedBounds, cellX, cellY)
                                && button.Enabled
                                && button.Visible;
            ClearPointerPress();

            if (releaseInside)
                button.Click(button, EventArgs.Empty);
            return true;
        }

        private int HitListRow(ListView listView, int cellX, int cellY)
        {
            var content = InsetPanelContent(listView.ArrangedBounds);
            if (content.Width <= 0 || content.Height <= 0)
                return -1;
            if (cellX < content.X || cellX >= content.X + content.Width)
                return -1;
            if (cellY < content.Y || cellY >= content.Y + content.Height)
                return -1;

            var index = GetListScroll(listView) + (cellY - content.Y);
            if (index < 0 || index >= listView.Count)
                return -1;
            return index;
        }

        private void ClearPointerPress()
        {
            _pressedButton = null;
            _pressedHot = false;
            _pressedListView = null;
            _listPressIndex = -1;
            _listPressWasSelected = false;
        }

        private static bool ContainsCell(LayoutRect bounds, int cellX, int cellY)
            => bounds.Width > 0
               && bounds.Height > 0
               && cellX >= bounds.X
               && cellX < bounds.X + bounds.Width
               && cellY >= bounds.Y
               && cellY < bounds.Y + bounds.Height;

        private bool HandleMouseWheel(KozuiInput input)
        {
            if (input.WheelDelta == 0)
                return false;

            PixelToCell(input.X, input.Y, out var cellX, out var cellY);
            var hit = HitTestInteractive(_root, cellX, cellY) as ListView
                      ?? FocusedControl() as ListView;
            if (hit == null || !hit.Enabled || hit.Count <= 0)
                return false;

            FocusControl(hit);
            var delta = input.WheelDelta > 0 ? -1 : 1;
            hit.SelectedIndex = Math.Max(0, Math.Min(hit.Count - 1, hit.SelectedIndex + delta));
            EnsureListVisible(hit, ListContentRows(hit));
            return true;
        }

        private void FocusControl(KozuiControl control)
        {
            if (control == null)
                return;
            _needsInitialFocus = false;
            var idx = _focusables.IndexOf(control);
            if (idx >= 0)
                _focusIndex = idx;
        }

        private void PixelToCell(int pixelX, int pixelY, out int cellX, out int cellY)
        {
            var cellW = TerminalFont.GlyphWidth * _scale;
            var cellH = TerminalFont.GlyphHeight * _scale;
            cellX = cellW > 0 ? pixelX / cellW : 0;
            cellY = cellH > 0 ? pixelY / cellH : 0;
        }

        private static KozuiControl HitTestInteractive(KozuiControl control, int cellX, int cellY)
        {
            if (control == null || !control.Visible)
                return null;

            // Deepest-first so nested detail controls win over parent panels.
            if (control is Panel panel)
            {
                for (var i = panel.Children.Count - 1; i >= 0; i--)
                {
                    var hit = HitTestInteractive(panel.Children[i], cellX, cellY);
                    if (hit != null)
                        return hit;
                }
            }
            else if (control is Placeholder placeholder && placeholder.Content != null)
            {
                var hit = HitTestInteractive(placeholder.Content, cellX, cellY);
                if (hit != null)
                    return hit;
            }

            if (!IsInteractive(control) || !control.Enabled)
                return null;

            var b = control.ArrangedBounds;
            if (cellX >= b.X && cellX < b.X + b.Width && cellY >= b.Y && cellY < b.Y + b.Height)
                return control;
            return null;
        }

        private static bool IsInteractive(KozuiControl control)
            => control is Button || control is CheckBox || control is TrackBar || control is ListView || control is TextBox;

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

        private int GetListScroll(ListView listView)
        {
            if (listView == null)
                return 0;
            return _listScrollByView.TryGetValue(listView, out var scroll) ? scroll : 0;
        }

        private void SetListScroll(ListView listView, int scroll)
        {
            if (listView == null)
                return;
            if (scroll <= 0)
                _listScrollByView.Remove(listView);
            else
                _listScrollByView[listView] = scroll;
        }

        private void EnsureListVisible(ListView listView, int visible)
        {
            if (listView == null)
                return;

            var count = listView.Count;
            visible = Math.Max(1, visible);
            var scroll = GetListScroll(listView);

            // No scrolling needed when everything fits.
            if (count <= visible)
            {
                SetListScroll(listView, 0);
                return;
            }

            var selected = listView.SelectedIndex;
            if (selected < 0)
                selected = 0;

            if (selected < scroll)
                scroll = selected;
            else if (selected >= scroll + visible)
                scroll = selected - visible + 1;

            var maxScroll = Math.Max(0, count - visible);
            if (scroll < 0)
                scroll = 0;
            else if (scroll > maxScroll)
                scroll = maxScroll;

            SetListScroll(listView, scroll);
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

            if (focused is ListView list && list.Enabled)
                list.ActivateItem();
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

            if (_pressedButton != null
                && (!_pressedButton.Visible || !_pressedButton.Enabled || !_focusables.Contains(_pressedButton)))
            {
                ClearPointerPress();
            }
            else if (_pressedListView != null
                     && (!_pressedListView.Visible || !_pressedListView.Enabled || !_focusables.Contains(_pressedListView)))
            {
                ClearPointerPress();
            }

            if (_needsInitialFocus)
            {
                // Before the first arrange, bounds are empty and tree order would
                // focus toolbar buttons. Wait until layout exists, then prefer a list.
                if (_focusables.Exists(c => c.ArrangedBounds.Width > 0 && c.ArrangedBounds.Height > 0))
                {
                    var textIndex = _focusables.FindIndex(c => c is TextBox);
                    var listIndex = _focusables.FindIndex(c => c is ListView);
                    if (textIndex >= 0)
                        _focusIndex = textIndex;
                    else if (listIndex >= 0)
                        _focusIndex = listIndex;
                    else
                        _focusIndex = 0;
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
            else if (control is TextBox textBox && textBox.Enabled)
                list.Add(textBox);

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

            int px, py;

            CellToPixel(bounds.X, bounds.Y, out px, out py);
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

            if (control is TextBox textBox)
            {
                DrawTextBox(textBox);
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

            if (control is ImageView imageView)
            {
                DrawImageView(imageView);
                return;
            }

            if (control is Placeholder placeholder)
            {
                // Modal overlays always paint frame chrome so the dialog reads as a window.
                var showChrome = IsActiveRegion(placeholder) || _terminal.HasBackdrop;
                if (showChrome)
                    DrawPanelChrome(placeholder.ArrangedBounds, active: true);
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
            int px, py;
            CellToPixel(bounds.X, bounds.Y, out px, out py);
            _terminal.DrawText(px, py, text, _scale, label.Enabled ? Fg : Disabled);
        }

        private void DrawImageView(ImageView imageView)
        {
            var bounds = imageView.ArrangedBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            int px, py;
            CellToPixel(bounds.X, bounds.Y, out px, out py);
            var slotW = bounds.Width * TerminalFont.GlyphWidth * _scale;
            var slotH = bounds.Height * TerminalFont.GlyphHeight * _scale;
            _terminal.FillRect(px, py, slotW, slotH, PanelBg);

            if (ImagePainter == null || imageView.Image == null)
            {
                var fallback = Truncate("[image]", bounds.Width);
                _terminal.DrawText(px, py, fallback, _scale, Disabled);
                return;
            }

            var srcW = imageView.SourceWidth > 0 ? imageView.SourceWidth : slotW;
            var srcH = imageView.SourceHeight > 0 ? imageView.SourceHeight : slotH;
            FitRect(slotW, slotH, srcW, srcH, out var drawW, out var drawH);
            var dx = px + (slotW - drawW) / 2;
            var dy = py + (slotH - drawH) / 2;

            try
            {
                using (var stream = imageView.Image())
                {
                    if (stream != null)
                        ImagePainter.DrawPng(dx, dy, drawW, drawH, imageView.ImageKey ?? "image", stream);
                }
            }
            catch
            {
                var fallback = Truncate("[image error]", bounds.Width);
                _terminal.DrawText(px, py, fallback, _scale, Disabled);
            }
        }

        private static void FitRect(int slotW, int slotH, int srcW, int srcH, out int drawW, out int drawH)
        {
            if (srcW <= 0 || srcH <= 0)
            {
                drawW = Math.Max(1, slotW);
                drawH = Math.Max(1, slotH);
                return;
            }

            var scale = Math.Min((double)slotW / srcW, (double)slotH / srcH);
            drawW = Math.Max(1, (int)Math.Round(srcW * scale));
            drawH = Math.Max(1, (int)Math.Round(srcH * scale));
        }

        private void DrawTextBox(TextBox textBox)
        {
            var bounds = textBox.ArrangedBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            var focused = ReferenceEquals(textBox, FocusedControl());
            int px, py;
            CellToPixel(bounds.X, bounds.Y, out px, out py);
            var pw = bounds.Width * TerminalFont.GlyphWidth * _scale;
            var ph = bounds.Height * TerminalFont.GlyphHeight * _scale;
            _terminal.FillRect(px, py, pw, ph, focused ? SelectedBg : PanelBg);

            var raw = textBox.Text ?? string.Empty;
            var shown = focused ? raw + "_" : raw;
            var text = Truncate(shown, bounds.Width);
            var color = !textBox.Enabled ? Disabled : focused ? Accent : Fg;
            _terminal.DrawText(px, py, text, _scale, color);
        }

        private void DrawButton(Button button)
        {
            var bounds = button.ArrangedBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            var focused = ReferenceEquals(button, FocusedControl());
            var pressed = ReferenceEquals(button, _pressedButton) && _pressedHot;
            int px, py;
            CellToPixel(bounds.X, bounds.Y, out px, out py);
            var pw = bounds.Width * TerminalFont.GlyphWidth * _scale;
            var ph = bounds.Height * TerminalFont.GlyphHeight * _scale;

            if (pressed)
                _terminal.FillRect(px, py, pw, ph, PressedBg);
            else if (focused)
                _terminal.FillRect(px, py, pw, ph, SelectedBg);

            var label = button.Text ?? string.Empty;
            string framed;
            if (pressed)
                framed = $"| {label} |";
            else if (focused)
                framed = $"> {label} <";
            else
                framed = $"[ {label} ]";
            framed = Truncate(framed, bounds.Width);
            var color = !button.Enabled ? Disabled : (pressed || focused) ? Accent : Fg;
            _terminal.DrawText(px, py, framed, _scale, color);
        }

        private void DrawCheckBox(CheckBox checkBox)
        {
            var bounds = checkBox.ArrangedBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            var focused = ReferenceEquals(checkBox, FocusedControl());
            int px, py;
            CellToPixel(bounds.X, bounds.Y, out px, out py);
            var pw = bounds.Width * TerminalFont.GlyphWidth * _scale;
            var ph = bounds.Height * TerminalFont.GlyphHeight * _scale;
            if (focused)
                _terminal.FillRect(px, py, pw, ph, SelectedBg);

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

            int px, py;

            CellToPixel(bounds.X, bounds.Y, out px, out py);
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
            int px, py;
            CellToPixel(bounds.X, bounds.Y, out px, out py);
            var pw = bounds.Width * TerminalFont.GlyphWidth * _scale;
            var ph = Math.Max(TerminalFont.GlyphHeight * _scale - 2, 4);
            if (focused)
                _terminal.FillRect(px, py, pw, ph, SelectedBg);

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
            var scroll = GetListScroll(listView);
            var cellW = TerminalFont.GlyphWidth * _scale;
            var cellH = TerminalFont.GlyphHeight * _scale;

            for (var row = 0; row < visible; row++)
            {
                var index = scroll + row;
                if (index >= listView.Count)
                    break;

                var y = content.Y + row;
                int px, py;
                CellToPixel(content.X, y, out px, out py);
                var pw = content.Width * cellW;
                var selected = index == listView.SelectedIndex;
                var spans = listView.GetHighlightSpans?.Invoke(index);
                var rowColors = listView.GetRowColors?.Invoke(index);
                var rowHighlight = selected && !listView.SuppressRowHighlight;
                // Selection overrides row tint (same order as WinForms DasmPanel).
                if (rowHighlight)
                    _terminal.FillRect(px, py, pw, cellH, SelectedBg);
                else if (rowColors.HasValue)
                    _terminal.FillRect(
                        px,
                        py,
                        pw,
                        cellH,
                        TerminalColor.Rgb(rowColors.Value.BgR, rowColors.Value.BgG, rowColors.Value.BgB));

                var prefix = rowHighlight ? ">" : " ";
                var itemText = listView.GetItemText(index) ?? string.Empty;
                var text = Truncate(prefix + itemText, content.Width);
                var color = !listView.Enabled
                    ? Disabled
                    : rowHighlight
                        ? Accent
                        : rowColors.HasValue
                            ? TerminalColor.Rgb(rowColors.Value.FgR, rowColors.Value.FgG, rowColors.Value.FgB)
                            : Fg;
                _terminal.DrawText(px, py, text, _scale, color);

                if (spans == null || spans.Count == 0 || !listView.Enabled)
                    continue;

                for (var s = 0; s < spans.Count; s++)
                {
                    var span = spans[s];
                    // +1 for the focus/spacer prefix drawn above.
                    var start = 1 + span.Start;
                    if (start >= text.Length || span.Length <= 0)
                        continue;
                    var length = Math.Min(span.Length, text.Length - start);
                    if (length <= 0)
                        continue;

                    int hx, hy;
                    CellToPixel(content.X + start, y, out hx, out hy);
                    _terminal.FillRect(hx, hy, length * cellW, cellH, SelectedBg);
                    _terminal.DrawText(hx, hy, text.Substring(start, length), _scale, Accent);
                }
            }

            if (focused && listView.Count == 0)
            {
                int px, py;
                CellToPixel(content.X, content.Y, out px, out py);
                _terminal.DrawText(px, py, "(no blocks)", _scale, Disabled);
            }
        }

        private void CellToPixel(int cellX, int cellY, out int pixelX, out int pixelY)
        {
            pixelX = cellX * TerminalFont.GlyphWidth * _scale;
            pixelY = cellY * TerminalFont.GlyphHeight * _scale;
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
    }
}
