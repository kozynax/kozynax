using System;
using System.Collections.Generic;
using Kozynax.UI;

namespace ZXMAK2.Host.Terminal
{
    /// <summary>
    /// Composes an optional <see cref="ToolbarStrip"/> above a <see cref="MenuBarScreen"/>
    /// in one input/draw loop. Either piece can be omitted.
    /// </summary>
    public sealed class MenuChromeScreen
    {
        private readonly ITerminal _terminal;
        private readonly object _commandParameter;
        private readonly int _scale;

        public MenuChromeScreen(ITerminal terminal, object commandParameter = null, int scale = 1)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            _commandParameter = commandParameter;
            _scale = Math.Max(1, scale);
        }

        /// <summary>
        /// Optional live background drawn before chrome (e.g. emulator frame).
        /// </summary>
        public Action Underlay { get; set; }

        public IList<MenuToolbarItem> Toolbar { get; set; }

        public IMenuImagePainter ImagePainter { get; set; }

        public bool ClosedByUser { get; private set; }

        public MenuBarCloseReason Run(MenuNode root)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            if (!_terminal.IsAvailable)
                return MenuBarCloseReason.Dismissed;

            var toolbar = new ToolbarStrip(_terminal, _commandParameter, _scale)
            {
                Items = Toolbar,
                ImagePainter = ImagePainter,
            };
            toolbar.Begin();

            var menu = new MenuBarScreen(_terminal, _commandParameter, _scale)
            {
                TopOffset = toolbar.Height,
                OwnsFrame = false,
            };
            menu.Begin(root);

            ClosedByUser = false;

            while (true)
            {
                while (_terminal.PollEvent(out var ev))
                {
                    var reason = HandleEvent(ev, toolbar, menu);
                    if (reason.HasValue)
                    {
                        ClosedByUser = menu.ClosedByUser || toolbar.ClosedByUser;
                        return reason.Value;
                    }
                }

                if (Underlay != null)
                    Underlay();
                else
                    _terminal.Clear(TerminalColor.Rgb(16, 18, 28));

                toolbar.DrawContent();
                menu.DrawContent();
                _terminal.Present();
                TerminalUiSession.AfterFrame(_terminal);
            }
        }

        private static MenuBarCloseReason? HandleEvent(
            TerminalEvent ev,
            ToolbarStrip toolbar,
            MenuBarScreen menu)
        {
            switch (ev.Kind)
            {
                case TerminalEventKind.Quit:
                case TerminalEventKind.KeyDown:
                    return menu.ProcessEvent(ev);

                case TerminalEventKind.MouseMove:
                    toolbar.HandleMouseMove(ev.X, ev.Y);
                    return menu.ProcessEvent(ev);

                case TerminalEventKind.MouseDown:
                    if (ev.Button == TerminalMouseButton.Right)
                        return menu.ProcessEvent(ev);
                    if (ev.Button == TerminalMouseButton.Left
                        && (toolbar.ContainsY(ev.Y) || toolbar.HasPressed))
                        return toolbar.HandleMouseDown(ev.X, ev.Y);
                    return menu.ProcessEvent(ev);

                case TerminalEventKind.MouseUp:
                    if (ev.Button == TerminalMouseButton.Left && toolbar.HasPressed)
                        return toolbar.HandleMouseUp(ev.X, ev.Y);
                    return menu.ProcessEvent(ev);

                default:
                    return null;
            }
        }
    }
}
