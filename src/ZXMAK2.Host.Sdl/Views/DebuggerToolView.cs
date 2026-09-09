using System;
using System.ComponentModel;
using Kozui.Interfaces;
using Kozynax.UI;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Presentation.Interfaces;
using ZXMAK2.Host.SdlBackend;
using ZXMAK2.Host.Terminal;

namespace ZXMAK2.Host.SdlBackend.Views
{
    /// <summary>
    /// Thin <see cref="IDebuggerGeneralView"/> adapter: wires lifecycle/breakpoint events
    /// and shows <see cref="DebuggerDialog"/> via <see cref="TerminalKozuiDialogHost{T}"/>
    /// with a <see cref="DebuggerTerminalSession"/>.
    /// </summary>
    public class DebuggerToolView : IDebuggerGeneralView
    {
        private readonly ITerminal _terminal;
        private readonly SdlRuntimeContext _runtime;
        private DebuggerDialog _dialog;
        private ISynchronizeInvoke _sync;
        private IMainView _parent;
        private DebuggerTerminalSession _session;
        private bool _loopActive;
        private bool _resumeOnClose;
        private bool _wired;

        public DebuggerToolView(ITerminal terminal, SdlRuntimeContext runtime)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            _runtime = runtime;
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

            _parent = parent ?? _parent;
            _sync = parent as ISynchronizeInvoke;
            _loopActive = true;

            _session = new DebuggerTerminalSession(_terminal, _dialog, TryRequestClose);
            var host = new TerminalKozuiDialogHost<DebuggerDialog>(_terminal, _runtime)
            {
                Session = _session,
            };
            host.Init(_dialog);

            try
            {
                host.ShowDialog(parent);
            }
            finally
            {
                _loopActive = false;
                _session = null;
                host.Dispose();
                TryResumeAfterClose();
            }
        }

        private bool TryRequestClose()
        {
            var args = new CancelEventArgs();
            ViewClosing?.Invoke(this, args);
            if (!args.Cancel)
                Close();
            else
                Hide();
            return true;
        }

        private void TryResumeAfterClose()
        {
            if (!_resumeOnClose)
                return;
            _resumeOnClose = false;
            var target = _dialog?.Target;
            if (target == null || target.IsRunning)
                return;
            try
            {
                target.DoRun();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public void Hide()
            => _session?.RequestClose();

        public void Close()
        {
            _session?.RequestClose();
            Unwire();
            _dialog?.Close();
            ViewClosed?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose()
        {
            Unwire();
            _dialog = null;
            _session = null;
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
            => _session?.RequestClose();

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

                if (_loopActive)
                {
                    _dialog.UpdateCPU(true);
                    return;
                }

                if (_parent != null)
                {
                    _resumeOnClose = true;
                    Show(_parent);
                }
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
    }
}
