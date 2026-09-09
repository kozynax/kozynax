using System;
using System.Collections.Generic;
using Kozynax.UI;
using ZXMAK2.Host.Terminal;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Mvvm;

namespace ZXMAK2.Host.SdlBackend.Views
{
    /// <summary>
    /// Debugger-specific Terminal input (F-keys, panel nav, mouse, context menus).
    /// </summary>
    internal sealed class DebuggerTerminalSession : IKozuiTerminalSession
    {
        private readonly ITerminal _terminal;
        private readonly DebuggerDialog _dialog;
        private readonly Func<bool> _tryClose;
        private bool _closeRequested;
        private int _dataClickTick;
        private int _dataClickRow = -1;
        private int _dataClickCol = -1;
        private int _dasmClickTick;
        private int _dasmClickRow = -1;
        private int _sideClickTick;
        private int _sideClickRow = -1;
        private object _sideClickList;

        public DebuggerTerminalSession(ITerminal terminal, DebuggerDialog dialog, Func<bool> tryClose)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            _dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
            _tryClose = tryClose ?? throw new ArgumentNullException(nameof(tryClose));
        }

        public bool ShouldClose => _closeRequested;

        public void RequestClose() => _closeRequested = true;

        public void OnSessionStart(object owner, TerminalKozuiPresenter presenter)
            => _dialog.UpdateCPU(true);

        public void OnSessionEnd()
        {
        }

        public void BeforeRender(TerminalKozuiPresenter presenter)
        {
            var dasmRows = Math.Max(0, _dialog.DasmList.ArrangedBounds.Height - 2);
            if (dasmRows > 0)
                _dialog.FitVisibleLines(dasmRows);
        }

        public bool TryHandleEvent(TerminalKozuiPresenter presenter, TerminalEvent ev)
        {
            if (ev.Kind == TerminalEventKind.Quit)
            {
                _tryClose();
                _closeRequested = true;
                return true;
            }

            if (ev.Kind == TerminalEventKind.KeyDown && HandleDebugKey(ev.Key))
                return true;

            if (ev.Kind == TerminalEventKind.KeyDown && TryHandleGotoAddress(presenter, ev))
                return true;

            if (ev.Kind == TerminalEventKind.KeyDown && TryHandleHexBlockIo(presenter, ev))
                return true;

            if (ev.Kind == TerminalEventKind.KeyDown && TryHandlePanelKey(presenter, ev.Key))
                return true;

            if (ev.Kind == TerminalEventKind.MouseDown
                && ev.Button == TerminalMouseButton.Right
                && TryShowPanelContextMenu(presenter, ev))
                return true;

            if (ev.Kind == TerminalEventKind.MouseDown
                && TryHandleDataMouseDown(presenter, ev, out var consumeMouse)
                && consumeMouse)
                return true;

            if (ev.Kind == TerminalEventKind.MouseDown
                && TryHandleDasmMouseDown(presenter, ev, out var consumeDasm)
                && consumeDasm)
                return true;

            if (ev.Kind == TerminalEventKind.MouseDown
                && TryHandleSideListMouseDown(presenter, ev, out var consumeSide)
                && consumeSide)
                return true;

            if (ev.Kind == TerminalEventKind.MouseWheel && TryHandlePanelWheel(presenter, ev))
                return true;

            if (TerminalDialogInput.IsEscape(ev))
            {
                _tryClose();
                _closeRequested = true;
                return true;
            }

            return false;
        }

        private bool TryHandleGotoAddress(TerminalKozuiPresenter presenter, TerminalEvent ev)
        {
            if (presenter == null || !ev.Ctrl || ev.Key != TerminalKey.G)
                return false;

            var focused = presenter.Focused;
            if (ReferenceEquals(focused, _dialog.DasmList))
            {
                _dialog.DasmGoToAddress();
                return true;
            }

            if (ReferenceEquals(focused, _dialog.DataList))
            {
                _dialog.DataGoToAddress();
                return true;
            }

            return false;
        }

        private bool TryHandleHexBlockIo(TerminalKozuiPresenter presenter, TerminalEvent ev)
        {
            if (presenter == null || !ev.Ctrl)
                return false;
            if (!ReferenceEquals(presenter.Focused, _dialog.DataList))
                return false;

            if (ev.Key == TerminalKey.L)
            {
                _dialog.LoadMemoryBlock();
                return true;
            }

            if (ev.Key == TerminalKey.S)
            {
                _dialog.SaveMemoryBlock();
                return true;
            }

            return false;
        }

        private bool TryHandlePanelKey(TerminalKozuiPresenter presenter, TerminalKey key)
        {
            if (presenter == null)
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
                    case TerminalKey.Space:
                    case TerminalKey.Enter:
                        _dialog.ToggleSelectedDasmBreakpoint();
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

        private bool TryHandleDataMouseDown(
            TerminalKozuiPresenter presenter,
            TerminalEvent ev,
            out bool consume)
        {
            consume = false;
            if (presenter == null || ev.Button != TerminalMouseButton.Left)
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

        private bool TryShowPanelContextMenu(TerminalKozuiPresenter presenter, TerminalEvent ev)
        {
            if (presenter == null)
                return false;

            IReadOnlyList<MenuNode> items = null;
            if (IsPointOver(_dialog.DasmList, ev.X, ev.Y))
            {
                if (TryHitDasmRow(ev.X, ev.Y, out var dasmRow))
                    _dialog.DasmSelectLine(dasmRow);
                presenter.Focus(_dialog.DasmList);
                items = BuildDasmContextMenu();
            }
            else if (IsPointOver(_dialog.DataList, ev.X, ev.Y))
            {
                if (TryHitDataCell(ev.X, ev.Y, out var dataRow, out var dataCol))
                    _dialog.DataSelectCell(dataRow, dataCol);
                presenter.Focus(_dialog.DataList);
                items = BuildDataContextMenu();
            }
            else
            {
                return false;
            }

            var menu = new ContextMenuScreen(_terminal)
            {
                Underlay = () =>
                {
                    var dasmRows = Math.Max(0, _dialog.DasmList.ArrangedBounds.Height - 2);
                    if (dasmRows > 0)
                        _dialog.FitVisibleLines(dasmRows);
                    presenter.DrawFrame();
                },
            };
            menu.Run(items, ev.X, ev.Y);
            return true;
        }

        private IReadOnlyList<MenuNode> BuildDasmContextMenu()
            => new[]
            {
                Cmd("Goto address...", () => _dialog.DasmGoToAddress()),
                Cmd("Goto PC", () => _dialog.DasmGoToPC()),
                MenuPopupOverlay.SeparatorNode(),
                Cmd("Reset breakpoints", () => _dialog.ClearBreakpoints()),
                MenuPopupOverlay.SeparatorNode(),
                Cmd("Load Block...", () => _dialog.LoadMemoryBlock()),
                Cmd("Save Block...", () => _dialog.SaveMemoryBlock()),
                MenuPopupOverlay.SeparatorNode(),
                Cmd("Refresh", () => _dialog.DasmRefresh()),
            };

        private IReadOnlyList<MenuNode> BuildDataContextMenu()
            => new[]
            {
                Cmd("Goto Address...", () => _dialog.DataGoToAddress()),
                Cmd("Set column count...", () => _dialog.SetDataColumnCount()),
                MenuPopupOverlay.SeparatorNode(),
                Cmd("Load Block...", () => _dialog.LoadMemoryBlock()),
                Cmd("Save Block...", () => _dialog.SaveMemoryBlock()),
                MenuPopupOverlay.SeparatorNode(),
                Cmd("Refresh", () => _dialog.DataRefresh()),
            };

        private static MenuNode Cmd(string caption, Action action)
            => new MenuNode
            {
                Caption = caption,
                Command = new CommandDelegate(action, () => true, caption),
            };

        private bool TryHandleDasmMouseDown(
            TerminalKozuiPresenter presenter,
            TerminalEvent ev,
            out bool consume)
        {
            consume = false;
            if (presenter == null || ev.Button != TerminalMouseButton.Left)
                return false;
            if (!TryHitDasmRow(ev.X, ev.Y, out var row))
                return false;

            const int doubleClickMs = 500;
            _dialog.DasmSelectLine(row);
            presenter.Focus(_dialog.DasmList);

            var now = Environment.TickCount;
            var isDoubleClick = row == _dasmClickRow
                                && unchecked(now - _dasmClickTick) <= doubleClickMs;
            consume = true;
            if (isDoubleClick)
            {
                _dasmClickTick = 0;
                _dasmClickRow = -1;
                _dialog.ToggleSelectedDasmBreakpoint();
            }
            else
            {
                _dasmClickTick = now;
                _dasmClickRow = row;
            }

            return true;
        }

        private bool TryHitDasmRow(int pixelX, int pixelY, out int row)
        {
            row = -1;
            if (!IsPointOver(_dialog.DasmList, pixelX, pixelY))
                return false;

            const int cellH = 8;
            const int pad = 1;
            var bounds = _dialog.DasmList.ArrangedBounds;
            var cellY = pixelY / cellH;
            var contentY = bounds.Y + pad;
            var contentH = Math.Max(0, bounds.Height - pad * 2);
            row = cellY - contentY;
            if (row < 0 || row >= contentH || row >= _dialog.DasmPanel.VisibleLineCount)
                return false;
            return true;
        }

        private bool TryHandleSideListMouseDown(
            TerminalKozuiPresenter presenter,
            TerminalEvent ev,
            out bool consume)
        {
            consume = false;
            if (presenter == null || ev.Button != TerminalMouseButton.Left)
                return false;

            ListView list = null;
            if (TryHitListRow(_dialog.FlagsList, ev.X, ev.Y, out var row))
                list = _dialog.FlagsList;
            else if (TryHitListRow(_dialog.StatesList, ev.X, ev.Y, out row))
                list = _dialog.StatesList;
            else if (_dialog is SprinterDebuggerDialog sprinter
                     && TryHitListRow(sprinter.ExtendedVariables, ev.X, ev.Y, out row))
                list = sprinter.ExtendedVariables;
            else
                return false;

            const int doubleClickMs = 500;
            list.SelectedIndex = row;
            presenter.Focus(list);

            var now = Environment.TickCount;
            var isDoubleClick = ReferenceEquals(list, _sideClickList)
                                && row == _sideClickRow
                                && unchecked(now - _sideClickTick) <= doubleClickMs;
            consume = true;
            if (isDoubleClick)
            {
                _sideClickTick = 0;
                _sideClickRow = -1;
                _sideClickList = null;
                list.ActivateItem();
            }
            else
            {
                _sideClickTick = now;
                _sideClickRow = row;
                _sideClickList = list;
            }

            return true;
        }

        private static bool TryHitListRow(ListView list, int pixelX, int pixelY, out int row)
        {
            row = -1;
            if (list == null || !IsPointOver(list, pixelX, pixelY))
                return false;

            const int cellH = 8;
            const int pad = 1;
            var bounds = list.ArrangedBounds;
            var cellY = pixelY / cellH;
            var contentY = bounds.Y + pad;
            var contentH = Math.Max(0, bounds.Height - pad * 2);
            row = cellY - contentY;
            if (row < 0 || row >= contentH || row >= list.Count)
                return false;
            return true;
        }

        private bool TryHitDataCell(int pixelX, int pixelY, out int row, out int col)
        {
            row = -1;
            col = 0;
            if (!IsPointOver(_dialog.DataList, pixelX, pixelY))
                return false;

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

            var relX = cellX - contentX - 1;
            if (relX >= 6)
                col = Math.Min(_dialog.DataPanel.ColCount - 1, Math.Max(0, (relX - 6) / 3));
            return true;
        }

        private bool TryHandlePanelWheel(TerminalKozuiPresenter presenter, TerminalEvent ev)
        {
            if (presenter == null || ev.WheelDelta == 0)
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

        private static bool IsPointOver(KozuiControl control, int pixelX, int pixelY)
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
