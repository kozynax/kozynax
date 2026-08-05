using System;
using System.Collections.Generic;
using Kozynax.UI;
using ZXMAK2.Host.Presentation;
using ZXMAK2.Host.Terminal;
using ZXMAK2.Mvvm;

namespace ZXMAK2.Host.SdlBackend.Views
{
    /// <summary>
    /// Terminal host for the Kozui <see cref="MainMenu"/> (F9 / auto-open on stdio).
    /// Reopens after settings/commands unless the user dismissed with Esc.
    /// </summary>
    public static class TerminalMainMenuView
    {
        public static void Show(
            ITerminal terminal,
            MainViewModel viewModel,
            IEnumerable<ICommand> toolCommands,
            object commandParameter,
            Func<bool> isHostQuitting = null)
        {
            if (terminal == null || !terminal.IsAvailable || viewModel == null)
                return;

            while (true)
            {
                if (isHostQuitting != null && isHostQuitting())
                    break;

                var menu = MainMenuFactory.Create(viewModel, toolCommands, commandParameter);
                terminal.PrepareForUiInput();
                var presenter = new TerminalKozuiPresenter(terminal);
                presenter.Attach(menu.Root);

                var closed = false;
                var quit = false;
                EventHandler onClose = (_, __) => closed = true;
                menu.CloseRequested += onClose;
                try
                {
                    TerminalUiSession.Run(
                        terminal,
                        presenter,
                        () => closed,
                        ev =>
                        {
                            if (ev.Kind == TerminalEventKind.Quit)
                            {
                                quit = true;
                                closed = true;
                                return true;
                            }

                            if (TerminalDialogInput.IsEscape(ev))
                            {
                                menu.TryGoBack();
                                return false;
                            }

                            TerminalDialogInput.Route(presenter, ev);
                            return false;
                        });
                }
                finally
                {
                    menu.CloseRequested -= onClose;
                    terminal.EndUiInput();
                }

                if (quit || menu.ClosedByUser || (isHostQuitting != null && isHostQuitting()))
                    break;
                // Command closed the menu after a nested dialog — reopen.
            }
        }
    }
}
