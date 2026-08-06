using System;
using Kozui.Interfaces;
using Kozynax.UI;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Terminal;
using ZXMAK2.Host.WinForms.Lib.Presenters;

namespace ZXMAK2.Host.SdlBackend.Views
{
    public sealed class TerminalInputDialogView : IViewImplementation<InputDialog>
    {
        private readonly ITerminal _terminal;
        private InputDialog _ui;

        public TerminalInputDialogView(ITerminal terminal)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        }

        public void Init(InputDialog ui)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        }

        public DlgResult ShowDialog(object owner)
        {
            if (_ui == null || !_terminal.IsAvailable)
                return DlgResult.Cancel;

            _terminal.PrepareForUiInput();
            _terminal.CaptureBackdrop();
            var presenter = new TerminalKozuiPresenter(_terminal);
            presenter.Attach(_ui.Root);

            var closed = false;
            EventHandler onClose = (_, __) => closed = true;
            _ui.CloseRequested += onClose;
            try
            {
                TerminalUiSession.Run(
                    _terminal,
                    presenter,
                    () => closed,
                    ev =>
                    {
                        if (ev.Kind == TerminalEventKind.Quit)
                        {
                            _ui.Cancel();
                            closed = true;
                            return true;
                        }

                        if (TerminalDialogInput.IsEscape(ev))
                        {
                            _ui.Cancel();
                            closed = true;
                            return true;
                        }

                        if (TerminalDialogInput.Route(presenter, ev))
                            return false;

                        // Enter in the text field accepts (WinForms AcceptButton).
                        if (ev.Kind == TerminalEventKind.KeyDown
                            && TerminalKozuiPresenter.MapKey(ev.Key) == KozuiInputKey.Enter)
                        {
                            _ui.OkButton.Click(_ui.OkButton, EventArgs.Empty);
                            return true;
                        }

                        return false;
                    });

                return _ui.DialogResult;
            }
            finally
            {
                _ui.CloseRequested -= onClose;
                _terminal.ReleaseBackdrop();
                _terminal.EndUiInput();
            }
        }

        public void Dispose()
        {
        }
    }
}
