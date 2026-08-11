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

                        if (ev.Kind == TerminalEventKind.KeyDown && TryHandlePanelKey(presenter, ev.Key))
                            return true;

                        if (ev.Kind == TerminalEventKind.MouseWheel && TryHandlePanelWheel(presenter, ev))
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
                    },
                    beforeRender: () =>
                    {
                        if (_dialog == null)
                            return;
                        // List chrome uses 1-cell padding on each side (see TerminalKozuiPresenter).
                        var dasmRows = Math.Max(0, _dialog.DasmList.ArrangedBounds.Height - 2);
                        var dataRows = Math.Max(0, _dialog.DataList.ArrangedBounds.Height - 2);
                        if (dasmRows > 0 || dataRows > 0)
                            _dialog.FitVisibleLines(dasmRows, dataRows);
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

        private bool TryHandlePanelKey(TerminalKozuiPresenter presenter, TerminalKey key)
        {
            if (_dialog == null || presenter == null)
                return false;

            var focused = presenter.Focused;
            if (ReferenceEquals(focused, _dialog.DasmList))
            {
                switch (key)
                {
                    case TerminalKey.Up:
                        _dialog.DasmNavigateUp();
                        return true;
                    case TerminalKey.Down:
                        _dialog.DasmNavigateDown();
                        return true;
                    case TerminalKey.PageUp:
                        _dialog.DasmNavigatePageUp();
                        return true;
                    case TerminalKey.PageDown:
                        _dialog.DasmNavigatePageDown();
                        return true;
                }
            }

            if (ReferenceEquals(focused, _dialog.DataList))
            {
                switch (key)
                {
                    case TerminalKey.Up:
                        _dialog.DataNavigateUp();
                        return true;
                    case TerminalKey.Down:
                        _dialog.DataNavigateDown();
                        return true;
                    case TerminalKey.PageUp:
                        _dialog.DataNavigatePageUp();
                        return true;
                    case TerminalKey.PageDown:
                        _dialog.DataNavigatePageDown();
                        return true;
                }
            }

            return false;
        }

        private bool TryHandlePanelWheel(TerminalKozuiPresenter presenter, TerminalEvent ev)
        {
            if (_dialog == null || presenter == null || ev.WheelDelta == 0)
                return false;

            var overDasm = IsPointOver(_dialog.DasmList, ev.X, ev.Y);
            var overData = IsPointOver(_dialog.DataList, ev.X, ev.Y);
            var focused = presenter.Focused;

            if (overDasm || (!overData && ReferenceEquals(focused, _dialog.DasmList)))
            {
                if (ev.WheelDelta > 0)
                    _dialog.DasmNavigateUp();
                else
                    _dialog.DasmNavigateDown();
                return true;
            }

            if (overData || ReferenceEquals(focused, _dialog.DataList))
            {
                if (ev.WheelDelta > 0)
                    _dialog.DataNavigateUp();
                else
                    _dialog.DataNavigateDown();
                return true;
            }

            return false;
        }

        private static bool IsPointOver(
            ZXMAK2.Host.WinForms.Lib.KozuiControl control,
            int pixelX,
            int pixelY)
        {
            if (control == null)
                return false;
            var bounds = control.ArrangedBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return false;

            const int cellW = 8;
            const int cellH = 8;
            var cellX = pixelX / cellW;
            var cellY = pixelY / cellH;
            return cellX >= bounds.X
                   && cellX < bounds.X + bounds.Width
                   && cellY >= bounds.Y
                   && cellY < bounds.Y + bounds.Height;
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
