using System;
using System.Collections.Generic;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Host.WinForms.Lib.Layout;
using ZXMAK2.Host.WinForms.Lib.Presenters;

namespace ZXMAK2.Host.Terminal
{
    /// <summary>
    /// Arranges a Kozui tree in character cells and paints Label/Button via <see cref="ITerminal"/>.
    /// </summary>
    public sealed class TerminalKozuiPresenter : IKozuiPresenter
    {
        private static readonly TerminalColor Bg = TerminalColor.Rgb(16, 18, 28);
        private static readonly TerminalColor Fg = TerminalColor.Rgb(230, 230, 230);
        private static readonly TerminalColor Accent = TerminalColor.Rgb(240, 220, 120);
        private static readonly TerminalColor FocusBg = TerminalColor.Rgb(40, 70, 120);
        private static readonly TerminalColor Disabled = TerminalColor.Rgb(90, 95, 110);

        private readonly ITerminal _terminal;
        private readonly int _scale;
        private KozuiControl _root;
        private readonly List<Button> _focusables = new List<Button>();
        private int _focusIndex;

        public TerminalKozuiPresenter(ITerminal terminal, int scale = 1)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            _scale = Math.Max(1, scale);
        }

        public KozuiControl Root => _root;

        public void Attach(KozuiControl root)
        {
            _root = root;
            RebuildFocusables();
            _focusIndex = 0;
        }

        public void MeasureArrange(LayoutSize availableCells)
        {
            if (_root == null)
                return;
            LayoutEngine.MeasureArrange(_root, availableCells);
        }

        /// <summary>
        /// Measure/arrange using the terminal pixel size converted to cells.
        /// </summary>
        public void MeasureArrangeFromTerminal()
        {
            var cells = GetAvailableCells();
            MeasureArrange(cells);
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

            _terminal.Clear(Bg);
            DrawControl(_root);
            _terminal.Present();
        }

        public bool RouteInput(KozuiInput input)
        {
            if (_root == null || input.Kind != KozuiInputKind.KeyDown)
                return false;

            switch (input.Key)
            {
                case KozuiInputKey.Tab:
                case KozuiInputKey.Right:
                case KozuiInputKey.Down:
                    MoveFocus(1);
                    return true;
                case KozuiInputKey.Left:
                case KozuiInputKey.Up:
                    MoveFocus(-1);
                    return true;
                case KozuiInputKey.Enter:
                    ActivateFocused();
                    return true;
                case KozuiInputKey.Escape:
                    return false;
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
                case TerminalKey.Left: return KozuiInputKey.Left;
                case TerminalKey.Right: return KozuiInputKey.Right;
                case TerminalKey.Up: return KozuiInputKey.Up;
                case TerminalKey.Down: return KozuiInputKey.Down;
                default: return KozuiInputKey.None;
            }
        }

        private void MoveFocus(int delta)
        {
            if (_focusables.Count == 0)
                return;
            _focusIndex = (_focusIndex + delta + _focusables.Count) % _focusables.Count;
        }

        private void ActivateFocused()
        {
            var button = FocusedButton();
            if (button != null && button.Enabled)
                button.Click(button, EventArgs.Empty);
        }

        private Button FocusedButton()
        {
            if (_focusables.Count == 0 || _focusIndex < 0 || _focusIndex >= _focusables.Count)
                return null;
            return _focusables[_focusIndex];
        }

        private void RebuildFocusables()
        {
            _focusables.Clear();
            if (_root != null)
                CollectFocusables(_root, _focusables);
            if (_focusIndex >= _focusables.Count)
                _focusIndex = Math.Max(0, _focusables.Count - 1);
        }

        private static void CollectFocusables(KozuiControl control, List<Button> list)
        {
            if (control == null || !control.Visible)
                return;

            if (control is Button button && button.Enabled)
                list.Add(button);

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

            if (control is Panel panel)
            {
                foreach (var child in panel.Children)
                    DrawControl(child);
                return;
            }

            if (control is Placeholder placeholder && placeholder.Content != null)
                DrawControl(placeholder.Content);
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

            var focused = ReferenceEquals(button, FocusedButton());
            var (px, py) = CellToPixel(bounds.X, bounds.Y);
            var (pw, ph) = (bounds.Width * TerminalFont.GlyphWidth * _scale,
                bounds.Height * TerminalFont.GlyphHeight * _scale);

            if (focused)
                _terminal.FillRect(px - 2, py - 1, pw + 4, ph + 2, FocusBg);

            var label = button.Text ?? string.Empty;
            var framed = focused ? $"> {label} <" : $"[ {label} ]";
            framed = Truncate(framed, bounds.Width);
            var color = !button.Enabled ? Disabled : focused ? Accent : Fg;
            _terminal.DrawText(px, py, framed, _scale, color);
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
