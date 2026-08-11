using System;
using System.ComponentModel;
using Kozynax.UI;
using ZXMAK2.Host.Presentation.Interfaces;
using ZXMAK2.Host.Terminal;

namespace ZXMAK2.Host.SdlBackend.Views
{
    /// <summary>
    /// Terminal host for the Kozui <see cref="AboutDialog"/>.
    /// </summary>
    public sealed class TerminalAboutView : IAboutView
    {
        private readonly ITerminal _terminal;
        private AboutDialog _ui;
        private bool _loopActive;
        private bool _closeRequested;

        public TerminalAboutView(ITerminal terminal)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        }

        public event EventHandler ViewClosed;
        public event CancelEventHandler ViewClosing;

        public void Show(IMainView parent)
        {
            if (!_terminal.IsAvailable || _loopActive)
                return;

            if (_ui == null)
                _ui = new AboutDialog();

            _closeRequested = false;
            _loopActive = true;
            _terminal.PrepareForUiInput();
            _terminal.CaptureBackdrop();
            var presenter = new TerminalKozuiPresenter(_terminal);
            presenter.Attach(_ui.Root);

            EventHandler onClose = (_, __) => _closeRequested = true;
            _ui.CloseRequested += onClose;
            try
            {
                TerminalUiSession.Run(
                    _terminal,
                    presenter,
                    () => _closeRequested,
                    ev =>
                    {
                        if (ev.Kind == TerminalEventKind.Quit)
                        {
                            Close();
                            return true;
                        }

                        if (TerminalDialogInput.IsEscape(ev))
                        {
                            var args = new CancelEventArgs();
                            ViewClosing?.Invoke(this, args);
                            if (!args.Cancel)
                                Close();
                            return true;
                        }

                        TerminalDialogInput.Route(presenter, ev);
                        return false;
                    });
            }
            finally
            {
                _ui.CloseRequested -= onClose;
                _loopActive = false;
                _terminal.ReleaseBackdrop();
                _terminal.EndUiInput();
            }
        }

        public void Hide()
            => _closeRequested = true;

        public void Close()
        {
            _closeRequested = true;
            _ui = null;
            ViewClosed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            _ui = null;
        }
    }
}
