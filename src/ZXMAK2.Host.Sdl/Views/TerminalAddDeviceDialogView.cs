using System;
using Kozui.Interfaces;
using Kozynax.UI;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Terminal;
using ZXMAK2.Host.WinForms.Lib.Presenters;

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

            var presenter = new TerminalKozuiPresenter(_terminal);
            presenter.Attach(_ui.Root);

            var closed = false;
            EventHandler onClose = (_, __) => closed = true;
            _ui.CloseRequested += onClose;
            try
            {
                while (!closed)
                {
                    while (_terminal.PollEvent(out var ev))
                    {
                        if (ev.Kind == TerminalEventKind.Quit)
                        {
                            _ui.RequestCancel();
                            return DlgResult.Cancel;
                        }

                        if (ev.Kind != TerminalEventKind.KeyDown)
                            continue;

                        var key = TerminalKozuiPresenter.MapKey(ev.Key);
                        if (key == KozuiInputKey.Escape)
                        {
                            _ui.RequestCancel();
                            return _ui.DialogResult;
                        }

                        presenter.RouteInput(KozuiInput.KeyDown(key));
                        if (closed)
                            break;
                    }

                    presenter.MeasureArrangeFromTerminal();
                    presenter.Render();
                    _terminal.Delay(16);
                }

                return _ui.DialogResult;
            }
            finally
            {
                _ui.CloseRequested -= onClose;
            }
        }

        public void Dispose()
        {
        }
    }
}
