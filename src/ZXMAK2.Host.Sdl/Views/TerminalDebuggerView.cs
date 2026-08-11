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
        private int _dataClickTick;
        private int _dataClickRow = -1;
        private int _dataClickCol = -1;

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

                        // Hex click: select byte (and poke on double-click). May fall through to Route.
                        if (ev.Kind == TerminalEventKind.MouseDown
                            && TryHandleDataMouseDown(presenter, ev, out var consumeMouse)
                            && consumeMouse)
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
                        var dasmRows = Math.Max(0, _dialog.DasmList.ArrangedBounds.Height - 2);
                        if (dasmRows > 0)
                            _dialog.FitVisibleLines(dasmRows);
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
                    case TerminalKey.Left:
                        _dialog.DataNavigateLeft();
                        return true;
                    case TerminalKey.Right:
                        _dialog.DataNavigateRight();
                        return true;
                    case TerminalKey.PageUp:
                        _dialog.DataNavigatePageUp();
                        return true;
                    case TerminalKey.PageDown:
                        _dialog.DataNavigatePageDown();
                        return true;
                    case TerminalKey.Enter:
                        _dialog.EditSelectedDataByte();
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Handles hex-panel mouse down. Returns false when the event is not over the hex list.
        /// When true, <paramref name="consume"/> indicates whether Route should be skipped.
        /// </summary>
        private bool TryHandleDataMouseDown(
            TerminalKozuiPresenter presenter,
            TerminalEvent ev,
            out bool consume)
        {
            consume = false;
            if (_dialog == null || presenter == null || ev.Button != TerminalMouseButton.Left)
                return false;
            if (!TryHitDataCell(ev.X, ev.Y, out var row, out var col))
                return false;

            const int doubleClickMs = 500;
            _dialog.DataSelectCell(row, col);
            presenter.Focus(_dialog.DataList);

            var now = Environment.TickCount;
            var isDoubleClick = row == _dataClickRow
                                && col == _dataClickCol
                                && unchecked(now - _dataClickTick) <= doubleClickMs;
            // Always consume hex clicks so the presenter cannot treat a second
            // single-click as activate — only a real double-click opens poke.
            consume = true;
            if (isDoubleClick)
            {
                _dataClickTick = 0;
                _dataClickRow = -1;
                _dataClickCol = -1;
                _dialog.EditSelectedDataByte();
            }
            else
            {
                _dataClickTick = now;
                _dataClickRow = row;
                _dataClickCol = col;
            }

            return true;
        }

        private bool TryHitDataCell(int pixelX, int pixelY, out int row, out int col)
        {
            row = -1;
            col = 0;
            if (_dialog == null || !IsPointOver(_dialog.DataList, pixelX, pixelY))
                return false;

            // Match DrawListView: 8x8 cells, 1-cell list chrome, leading focus prefix.
            const int cellW = 8;
            const int cellH = 8;
            const int pad = 1;
            var bounds = _dialog.DataList.ArrangedBounds;
            var cellX = pixelX / cellW;
            var cellY = pixelY / cellH;
            var contentX = bounds.X + pad;
            var contentY = bounds.Y + pad;
            var contentH = Math.Max(0, bounds.Height - pad * 2);
            row = cellY - contentY;
            if (row < 0 || row >= contentH || row >= _dialog.DataPanel.VisibleLineCount)
                return false;

            // Text: "XXXX  HH HH ...  cccc" after the focus prefix.
            var relX = cellX - contentX - 1;
            if (relX >= 6)
                col = Math.Min(_dialog.DataPanel.ColCount - 1, Math.Max(0, (relX - 6) / 3));
            return true;
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
