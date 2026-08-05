using System;
using Kozui.Interfaces;
using Kozynax.UI;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Terminal;

namespace ZXMAK2.Host.SdlBackend.Views
{
    public sealed class TerminalAddDeviceDialogView : IViewImplementation<AddDeviceDialog>
    {
        private readonly ITerminal _terminal;
        private AddDeviceDialog _ui;

        public TerminalAddDeviceDialogView(ITerminal terminal)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        }

        public void Init(AddDeviceDialog ui)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
        }

        public DlgResult ShowDialog(object owner)
        {
            if (_ui == null || !_terminal.IsAvailable)
                return DlgResult.Cancel;

            _terminal.PrepareForUiInput();
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
                            _ui.RequestCancel();
                            closed = true;
                            return true;
                        }

                        if (TerminalDialogInput.IsEscape(ev))
                        {
                            _ui.RequestCancel();
                            closed = true;
                            return true;
                        }

                        TerminalDialogInput.Route(presenter, ev);
                        return false;
                    });

                return _ui.DialogResult;
            }
            finally
            {
                _ui.CloseRequested -= onClose;
                _terminal.EndUiInput();
            }
        }

        public void Dispose()
        {
        }
    }
}
