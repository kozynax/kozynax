using System;
using System.Collections.Generic;
using Kozynax.UI;
using ZXMAK2.Host.Presentation;
using ZXMAK2.Host.Terminal;
using ZXMAK2.Mvvm;

namespace ZXMAK2.Host.SdlBackend.Views
{
    /// <summary>
    /// Terminal host for the Kozui <see cref="MainMenu"/> (F9).
    /// </summary>
    public static class TerminalMainMenuView
    {
        public static void Show(
            ITerminal terminal,
            MainViewModel viewModel,
            IEnumerable<ICommand> toolCommands,
            object commandParameter)
        {
            if (terminal == null || !terminal.IsAvailable || viewModel == null)
                return;

            var menu = MainMenuFactory.Create(viewModel, toolCommands, commandParameter);
            terminal.PrepareForUiInput();
            var presenter = new TerminalKozuiPresenter(terminal);
            presenter.Attach(menu.Root);

            var closed = false;
            EventHandler onClose = (_, __) => closed = true;
            menu.CloseRequested += onClose;
            try
            {
                while (!closed)
                {
                    while (terminal.PollEvent(out var ev))
                    {
                        if (ev.Kind == TerminalEventKind.Quit)
                            return;

                        if (TerminalDialogInput.IsEscape(ev))
                        {
                            menu.TryGoBack();
                            continue;
                        }

                        TerminalDialogInput.Route(presenter, ev);
                    }

                    presenter.MeasureArrangeFromTerminal();
                    presenter.Render();
                    terminal.Delay(16);
                }
            }
            finally
            {
                menu.CloseRequested -= onClose;
                terminal.EndUiInput();
            }
        }
    }
}
