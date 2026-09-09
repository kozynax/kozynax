using System;
using System.Collections.Generic;
using Kozynax.UI;

namespace ZXMAK2.Host.Terminal
{
    /// <summary>
    /// Image toolbar strip drawn through <see cref="ITerminal"/>.
    /// Usable alone or composed above a <see cref="MenuBarScreen"/>.
    /// </summary>
    public sealed class ToolbarStrip
    {
        private static readonly TerminalColor BarBg = TerminalColor.Rgb(28, 32, 48);
        private static readonly TerminalColor Border = TerminalColor.Rgb(55, 65, 90);
        private static readonly TerminalColor SelectedBg = TerminalColor.Rgb(30, 100, 210);
        private static readonly TerminalColor CheckedBg = TerminalColor.Rgb(18, 60, 140);

        private readonly ITerminal _terminal;
        private readonly object _commandParameter;
        private readonly int _scale;

        private readonly List<Slot> _slots = new List<Slot>();
        private int _hot = -1;
        private int _pressed = -1;
        private int _iconSize;
        private int _pad;
        private int _lastWidth = -1;

        public ToolbarStrip(ITerminal terminal, object commandParameter = null, int scale = 1)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            _commandParameter = commandParameter;
            _scale = Math.Max(1, scale);
        }

        public IList<MenuToolbarItem> Items { get; set; }

        public IMenuImagePainter ImagePainter { get; set; }

        /// <summary>Optional live background when running standalone.</summary>
        public Action Underlay { get; set; }

        /// <summary>Y origin of the strip (default 0).</summary>
        public int Top { get; set; }

        public int Height { get; private set; }

        public bool HasPressed => _pressed >= 0;

        public bool ClosedByUser { get; private set; }

        public bool ContainsY(int y)
            => Height > 0 && y >= Top && y < Top + Height;

        public void Begin()
        {
            ClosedByUser = false;
            _hot = -1;
            _pressed = -1;
            RebuildMetrics();
            Layout();
        }

        /// <summary>
        /// Standalone loop — show toolbar until dismiss / command / quit.
        /// </summary>
        public MenuBarCloseReason Run()
        {
            if (!_terminal.IsAvailable)
                return MenuBarCloseReason.Dismissed;

            Begin();

            while (true)
            {
                while (_terminal.PollEvent(out var ev))
                {
                    var reason = HandleStandaloneEvent(ev);
                    if (reason.HasValue)
                        return reason.Value;
                }

                if (Underlay != null)
                    Underlay();
                else
                    _terminal.Clear(TerminalColor.Rgb(16, 18, 28));

                DrawContent();
                _terminal.Present();
                TerminalUiSession.AfterFrame(_terminal);
            }
        }

        public MenuBarCloseReason? HandleMouseDown(int x, int y)
        {
            if (!ContainsY(y))
                return null;

            var hit = Hit(x, y);
            if (hit >= 0)
                _pressed = hit;
            return null;
        }

        public MenuBarCloseReason? HandleMouseUp(int x, int y)
        {
            if (_pressed < 0)
                return null;

            var pressed = _pressed;
            _pressed = -1;
            if (Hit(x, y) == pressed)
                return Activate(_slots[pressed].Item);
            return null;
        }

        public void HandleMouseMove(int x, int y)
        {
            _hot = Hit(x, y);
        }

        public void DrawContent()
        {
            RebuildMetrics();
            var winW = Math.Max(_terminal.Width, 1);
            if (winW != _lastWidth)
            {
                _lastWidth = winW;
                Layout();
            }

            if (Height <= 0)
                return;

            _terminal.FillRect(0, Top, winW, Height, BarBg);
            _terminal.FillRect(0, Top + Height - _scale, winW, _scale, Border);

            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];
                var item = slot.Item;
                if (item == null)
                    continue;

                if (item.IsSeparator)
                {
                    _terminal.FillRect(
                        slot.X + slot.Width / 2,
                        slot.Y,
                        Math.Max(1, _scale),
                        slot.Height,
                        Border);
                    continue;
                }

                var enabled = IsEnabled(item);
                var isHot = i == _hot || i == _pressed;
                var isChecked = item.IsChecked != null && item.IsChecked();
                if ((isHot && enabled) || isChecked)
                    _terminal.FillRect(slot.X, slot.Y, slot.Width, slot.Height, isChecked ? CheckedBg : SelectedBg);

                if (ImagePainter == null || item.Image == null || !enabled)
                    continue;

                try
                {
                    using (var stream = item.Image())
                    {
                        if (stream == null)
                            continue;
                        var key = item.ImageKey != null ? item.ImageKey() : item.Tip;
                        var ix = slot.X + (slot.Width - _iconSize) / 2;
                        var iy = slot.Y;
                        ImagePainter.DrawPng(ix, iy, _iconSize, _iconSize, key ?? item.Tip, stream);
                    }
                }
                catch
                {
                    // keep chrome usable even if one icon fails
                }
            }
        }

        private MenuBarCloseReason? HandleStandaloneEvent(TerminalEvent ev)
        {
            switch (ev.Kind)
            {
                case TerminalEventKind.Quit:
                    ClosedByUser = true;
                    return MenuBarCloseReason.Quit;

                case TerminalEventKind.KeyDown:
                    if (ev.Key == TerminalKey.F9 || ev.Key == TerminalKey.Escape)
                    {
                        ClosedByUser = true;
                        return MenuBarCloseReason.Dismissed;
                    }
                    return null;

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
                    {
                        if (!ContainsY(ev.Y))
                        {
                            ClosedByUser = true;
                            return MenuBarCloseReason.Dismissed;
                        }
                        return HandleMouseDown(ev.X, ev.Y);
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

        private void RebuildMetrics()
        {
            _pad = 4 * _scale;
            _iconSize = 32;
            Height = Items != null && Items.Count > 0
                ? _iconSize + _pad * 2
                : 0;
        }

        private void Layout()
        {
            _slots.Clear();
            if (Items == null || Items.Count == 0 || Height <= 0)
                return;

            var x = _pad;
            var y = Top + _pad;
            foreach (var item in Items)
            {
                if (item == null)
                    continue;
                if (item.IsSeparator)
                {
                    var sepW = Math.Max(2, _scale * 2);
                    _slots.Add(new Slot
                    {
                        Item = item,
                        X = x,
                        Y = y,
                        Width = sepW,
                        Height = _iconSize,
                    });
                    x += _pad + sepW;
                    continue;
                }

                var width = _iconSize + _pad;
                _slots.Add(new Slot
                {
                    Item = item,
                    X = x,
                    Y = y,
                    Width = width,
                    Height = _iconSize,
                });
                x += width + _pad / 2;
            }
        }

        private int Hit(int x, int y)
        {
            if (!ContainsY(y))
                return -1;
            for (var i = 0; i < _slots.Count; i++)
            {
                var s = _slots[i];
                if (s.Item == null || s.Item.IsSeparator)
                    continue;
                if (x >= s.X && x < s.X + s.Width && y >= s.Y && y < s.Y + s.Height)
                    return i;
            }
            return -1;
        }

        private MenuBarCloseReason? Activate(MenuToolbarItem item)
        {
            if (item == null || item.IsSeparator || item.Command == null)
                return null;

            var param = ResolveParameter(item.Parameter, item.Command, item.IsChecked != null);
            if (!item.Command.CanExecute(param))
                return null;

            item.Command.Execute(param);

            if (item.IsChecked != null)
                return null;

            ClosedByUser = false;
            return MenuBarCloseReason.CommandExecuted;
        }

        private bool IsEnabled(MenuToolbarItem item)
        {
            if (item?.Command == null)
                return false;
            return item.Command.CanExecute(ResolveParameter(item.Parameter, item.Command, item.IsChecked != null));
        }

        private object ResolveParameter(object parameter, ZXMAK2.Mvvm.ICommand command, bool isToggle)
        {
            if (parameter != null)
                return parameter;
            if (isToggle || (command != null && command.CanExecute(null)))
                return null;
            return _commandParameter;
        }

        private sealed class Slot
        {
            public MenuToolbarItem Item;
            public int X;
            public int Y;
            public int Width;
            public int Height;
        }
    }
}
