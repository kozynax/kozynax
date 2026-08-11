using System;
using System.ComponentModel;
using Kozynax.UI;
using ZXMAK2.Hardware.Circuits.Fdd;
using ZXMAK2.Host.Presentation.Interfaces;
using ZXMAK2.Host.Terminal;

namespace ZXMAK2.Host.SdlBackend.Views
{
    /// <summary>
    /// Terminal host for the Kozui <see cref="FddDebugDialog"/>.
    /// </summary>
    public sealed class TerminalFddDebugView : IFddDebugView
    {
        private readonly ITerminal _terminal;
        private FddDebugDialog _ui;
        private bool _loopActive;
        private bool _closeRequested;

        public TerminalFddDebugView(ITerminal terminal)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        }

        public event EventHandler ViewClosed;
        public event CancelEventHandler ViewClosing;

        public void Init(Wd1793 debugTarget)
        {
            _ui?.Close();
            _ui = new FddDebugDialog(debugTarget);
            _ui.CloseRequested += (_, __) => _closeRequested = true;
        }

        public void Show(IMainView parent)
        {
            if (_ui == null || !_terminal.IsAvailable || _loopActive)
                return;

            _closeRequested = false;
            _loopActive = true;
            _terminal.PrepareForUiInput();
            _terminal.CaptureBackdrop();
            var presenter = new TerminalKozuiPresenter(_terminal);
            presenter.Attach(_ui.Root);

            var lastTick = Environment.TickCount;
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
                    },
                    beforeRender: () =>
                    {
                        if (_ui == null || !_ui.UpdateTimer.Enabled)
                            return;
                        var now = Environment.TickCount;
                        var interval = Math.Max(50, _ui.UpdateTimer.IntervalMs);
                        if (unchecked(now - lastTick) >= interval)
                        {
                            lastTick = now;
                            _ui.UpdateTimer.Tick();
                        }
                    });
            }
            finally
            {
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
            if (_ui != null)
            {
                _ui.Close();
                _ui = null;
            }
            ViewClosed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            if (_ui != null)
            {
                _ui.Close();
                _ui = null;
            }
        }
    }
}
