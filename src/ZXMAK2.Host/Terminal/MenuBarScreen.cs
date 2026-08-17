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
    /// Compose with <see cref="ToolbarStrip"/> via <see cref="MenuChromeScreen"/> when both are needed.
    /// </summary>
    public sealed class MenuBarScreen
    {
        private static readonly TerminalColor BarBg = TerminalColor.Rgb(28, 32, 48);
        private static readonly TerminalColor Border = TerminalColor.Rgb(55, 65, 90);
        private static readonly TerminalColor Fg = TerminalColor.Rgb(230, 230, 230);
        private static readonly TerminalColor Accent = TerminalColor.Rgb(240, 220, 120);
        private static readonly TerminalColor SelectedBg = TerminalColor.Rgb(30, 100, 210);

        private readonly ITerminal _terminal;
        private readonly object _commandParameter;
        private readonly int _scale;
        private readonly MenuPopupOverlay _popups;

        private MenuNode _root;
        private readonly List<TopItem> _tops = new List<TopItem>();
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
        /// Ignored when <see cref="OwnsFrame"/> is false.
        /// </summary>
        public Action Underlay { get; set; }

        /// <summary>Y offset of the menu bar (e.g. below a toolbar).</summary>
        public int TopOffset { get; set; }

        /// <summary>
        /// When false, <see cref="DrawContent"/> is used by a host that owns clear/present
        /// (see <see cref="MenuChromeScreen"/>).
        /// </summary>
        public bool OwnsFrame { get; set; } = true;

        private int ChromeBottom => TopOffset + _barHeight;
        private int MenuY => TopOffset;

        public MenuBarScreen(ITerminal terminal, object commandParameter = null, int scale = 1)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            _commandParameter = commandParameter;
            _scale = Math.Max(1, scale);
            _popups = new MenuPopupOverlay(terminal, commandParameter, scale);
        }

        /// <summary>
        /// True when closed via F9 / Esc / outside click (not a command).
        /// </summary>
        public bool ClosedByUser { get; private set; }

        public void Begin(MenuNode root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));

            _root = root;
            ClosedByUser = false;
            _open = false;
            _popups.Clear();
            _topIndex = 0;
            RebuildMetrics();
            LayoutTops();
        }

        public MenuBarCloseReason Run(MenuNode root)
        {
            if (!_terminal.IsAvailable)
                return MenuBarCloseReason.Dismissed;

            Begin(root);

            while (true)
            {
                while (_terminal.PollEvent(out var ev))
                {
                    var reason = ProcessEvent(ev);
                    if (reason.HasValue)
                        return reason.Value;
                }

                DrawFrame();
                TerminalUiSession.AfterFrame(_terminal);
            }
        }

        public MenuBarCloseReason? ProcessEvent(TerminalEvent ev)
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

        public void DrawContent()
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

            _terminal.FillRect(0, MenuY, winW, _barHeight, BarBg);
            _terminal.FillRect(0, ChromeBottom - _scale, winW, _scale, Border);

            for (var i = 0; i < _tops.Count; i++)
            {
                var t = _tops[i];
                if (i == _topIndex)
                    _terminal.FillRect(t.X, MenuY, t.Width, _barHeight, SelectedBg);
                var color = i == _topIndex ? Accent : Fg;
                _terminal.DrawText(
                    t.X + _pad,
                    MenuY + (_barHeight - TerminalFont.GlyphHeight * _scale) / 2,
                    t.Text,
                    _scale,
                    color);
            }

            _popups.ClampMinY = ChromeBottom;
            _popups.Draw();
        }

        private void DrawFrame()
        {
            if (OwnsFrame)
            {
                if (Underlay != null)
                    Underlay();
                else
                    _terminal.Clear(TerminalColor.Rgb(16, 18, 28));
            }

            DrawContent();

            if (OwnsFrame)
                _terminal.Present();
        }

        private MenuBarCloseReason? HandleKey(TerminalKey key)
        {
            if (key == TerminalKey.F9)
            {
                ClosedByUser = true;
                return MenuBarCloseReason.Dismissed;
            }

            if (!_open)
            {
                if (key == TerminalKey.Escape)
                {
                    ClosedByUser = true;
                    return MenuBarCloseReason.Dismissed;
                }
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

            // Open: Left/Right without a nested child switches top menus (WinForms-style).
            if (key == TerminalKey.Left)
            {
                if (_popups.Depth > 1)
                {
                    _popups.ProcessEvent(TerminalEvent.KeyDown(TerminalKey.Left));
                    return null;
                }
                MoveTop(-1);
                OpenTop(_topIndex);
                return null;
            }

            if (key == TerminalKey.Right)
            {
                if (_popups.TryOpenSelectedChild())
                    return null;
                MoveTop(1);
                OpenTop(_topIndex);
                return null;
            }

            var overlayReason = _popups.ProcessEvent(TerminalEvent.KeyDown(key));
            if (overlayReason == MenuBarCloseReason.Dismissed && key == TerminalKey.Escape)
            {
                // Esc closed the last popup — return to top-bar browsing (not leave menu).
                _open = false;
                return null;
            }

            return MapOverlayReason(overlayReason);
        }

        private MenuBarCloseReason? MapOverlayReason(MenuBarCloseReason? reason)
        {
            if (reason == null)
                return null;
            if (reason == MenuBarCloseReason.Dismissed)
            {
                ClosedByUser = true;
                _open = false;
                _popups.Clear();
            }
            else if (reason == MenuBarCloseReason.CommandExecuted
                     || reason == MenuBarCloseReason.Quit)
            {
                ClosedByUser = reason == MenuBarCloseReason.Quit;
                _open = false;
                _popups.Clear();
            }
            return reason;
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

            if (y >= MenuY && y < ChromeBottom)
                return null;

            if (_open && _popups.ContainsPoint(x, y))
                return null;

            ClosedByUser = true;
            return MenuBarCloseReason.Dismissed;
        }

        private MenuBarCloseReason? HandleMouseUp(int x, int y)
        {
            if (!_open)
                return null;

            if (!_popups.HitTest(x, y, out var popupIndex, out var itemIndex))
                return null;

            return MapOverlayReason(_popups.ActivateAt(popupIndex, itemIndex));
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

            if (!_popups.HitTest(x, y, out var popupIndex, out var itemIndex))
                return;

            _popups.SelectItem(popupIndex, itemIndex);
        }

        private void OpenTop(int index)
        {
            if (index < 0 || index >= _tops.Count)
                return;

            _topIndex = index;
            _open = true;
            _popups.ClampMinY = ChromeBottom;

            var top = _tops[index];
            var items = top.Node.Children ?? new List<MenuNode>();
            if (items.Count == 0)
            {
                _popups.Clear();
                return;
            }

            _popups.OpenRoot(items, top.X, ChromeBottom);
        }

        private void ClosePopups()
        {
            _popups.Clear();
            _open = false;
        }

        private void MoveTop(int delta)
        {
            if (_tops.Count == 0)
                return;
            _topIndex = (_topIndex + delta + _tops.Count) % _tops.Count;
        }

        private int HitTop(int x, int y)
        {
            if (y < MenuY || y >= MenuY + _barHeight)
                return -1;
            for (var i = 0; i < _tops.Count; i++)
            {
                var t = _tops[i];
                if (x >= t.X && x < t.X + t.Width)
                    return i;
            }
            return -1;
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

        private sealed class TopItem
        {
            public MenuNode Node;
            public string Text;
            public int X;
            public int Width;
        }
    }
}
