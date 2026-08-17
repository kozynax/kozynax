using System;
using System.Collections.Generic;
using Kozynax.UI;

namespace ZXMAK2.Host.Terminal
{
    /// <summary>
    /// Context / right-click menu: same cascading popup chrome as the main menu bar,
    /// shown at a screen position over an optional underlay.
    /// </summary>
    public sealed class ContextMenuScreen
    {
        private readonly ITerminal _terminal;
        private readonly object _commandParameter;
        private readonly int _scale;

        public ContextMenuScreen(ITerminal terminal, object commandParameter = null, int scale = 1)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            _commandParameter = commandParameter;
            _scale = Math.Max(1, scale);
        }

        /// <summary>Drawn each frame before the popup (e.g. the host dialog).</summary>
        public Action Underlay { get; set; }

        public bool ClosedByUser { get; private set; }

        public MenuBarCloseReason Run(IReadOnlyList<MenuNode> items, int x, int y)
        {
            if (!_terminal.IsAvailable || items == null || items.Count == 0)
                return MenuBarCloseReason.Dismissed;

            var popups = new MenuPopupOverlay(_terminal, _commandParameter, _scale)
            {
                ClampMinY = 0,
            };
            popups.OpenRoot(items, x, y);
            ClosedByUser = false;

            while (popups.IsOpen)
            {
                while (_terminal.PollEvent(out var ev))
                {
                    var reason = popups.ProcessEvent(ev);
                    if (reason == null)
                        continue;

                    ClosedByUser = reason == MenuBarCloseReason.Dismissed
                                   || reason == MenuBarCloseReason.Quit;
                    popups.Clear();
                    return reason.Value;
                }

                if (Underlay != null)
                    Underlay();
                else
                    _terminal.Clear(TerminalColor.Rgb(16, 18, 28));

                popups.Draw();
                _terminal.Present();
                TerminalUiSession.AfterFrame(_terminal);
            }

            ClosedByUser = true;
            return MenuBarCloseReason.Dismissed;
        }
    }
}
