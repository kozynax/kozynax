using System;
using System.ComponentModel;
using Kozui.Interfaces;
using Kozynax.UI;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Presentation.Interfaces;
using ZXMAK2.Host.Terminal;

namespace ZXMAK2.Host.SdlBackend.Views
{
    /// <summary>
    /// Terminal/SDL host for the regular (non-Sprinter) <see cref="DebuggerDialog"/>.
    /// </summary>
    public sealed class TerminalDebuggerView : IDebuggerGeneralView
    {
        private readonly ITerminal _terminal;
        private DebuggerDialog _dialog;
        private ISynchronizeInvoke _sync;
        private bool _loopActive;
        private bool _closeRequested;
        private bool _wired;

        public TerminalDebuggerView(ITerminal terminal)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        }

        public event EventHandler ViewClosed;
        public event CancelEventHandler ViewClosing;

        public void Init(DebuggerDialog ui)
        {
            Unwire();
            _dialog = ui ?? throw new ArgumentNullException(nameof(ui));
            Wire();
        }

        public void Init(IDebuggable dbg)
        {
            if (_dialog == null)
                throw new InvalidOperationException("Call Init(DebuggerDialog) before Init(IDebuggable).");
            _dialog.Init(dbg);
        }

        public DlgResult ShowDialog(object owner)
        {
            Show(owner as IMainView);
            return DlgResult.OK;
        }

        public void Show(IMainView parent)
        {
            if (_dialog == null || !_terminal.IsAvailable || _loopActive)
                return;

            _sync = parent as ISynchronizeInvoke;
            _closeRequested = false;
            _loopActive = true;
            _terminal.PrepareForUiInput();
            _terminal.CaptureBackdrop();

            var presenter = new TerminalKozuiPresenter(_terminal);
            presenter.Attach(_dialog.Root);
            _dialog.UpdateCPU(true);

            var previousIdle = TerminalUiSession.IdlePump;
            var sdlView = parent as SdlMainView;
            TerminalUiSession.IdlePump = () =>
            {
                previousIdle?.Invoke();
                sdlView?.PumpUiCallbacks();
            };

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

                        if (ev.Kind == TerminalEventKind.KeyDown && HandleDebugKey(ev.Key))
                            return true;

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
                TerminalUiSession.IdlePump = previousIdle;
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
            Unwire();
            _dialog?.Close();
            ViewClosed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            Unwire();
            _dialog = null;
        }

        private void Wire()
        {
            if (_dialog == null || _wired)
                return;
            _dialog.CloseRequested += OnCloseRequested;
            _dialog.UpdateState += OnUpdateState;
            _dialog.Breakpoint += OnBreakpoint;
            _wired = true;
        }

        private void Unwire()
        {
            if (_dialog == null || !_wired)
                return;
            _dialog.CloseRequested -= OnCloseRequested;
            _dialog.UpdateState -= OnUpdateState;
            _dialog.Breakpoint -= OnBreakpoint;
            _wired = false;
        }

        private void OnCloseRequested(object sender, EventArgs e)
            => _closeRequested = true;

        private void OnUpdateState(object sender, EventArgs e)
            => InvokeOnUi(() =>
            {
                if (_dialog == null || !_loopActive)
                    return;
                _dialog.UpdateCPU(true);
            });

        private void OnBreakpoint(object sender, EventArgs e)
            => InvokeOnUi(() =>
            {
                if (_dialog == null)
                    return;
                // Breakpoint while view is closed: ViewHolder may re-Show; if already open, refresh.
                if (_loopActive)
                    _dialog.UpdateCPU(true);
            });

        private void InvokeOnUi(Action action)
        {
            if (action == null)
                return;
            if (_sync != null && _sync.InvokeRequired)
                _sync.BeginInvoke(action, null);
            else
                action();
        }

        private bool HandleDebugKey(TerminalKey key)
        {
            if (_dialog == null)
                return false;

            switch (key)
            {
                case TerminalKey.F3:
                    _dialog.ResetCpu();
                    return true;
                case TerminalKey.F5:
                    _dialog.Stop();
                    return true;
                case TerminalKey.F7:
                    _dialog.StepInto();
                    return true;
                case TerminalKey.F8:
                    _dialog.StepOver();
                    return true;
                case TerminalKey.F9:
                    _dialog.Run();
                    return true;
                default:
                    return false;
            }
        }
    }
}
