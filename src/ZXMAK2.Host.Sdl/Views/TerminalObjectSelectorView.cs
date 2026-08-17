using System;
using Kozui.Interfaces;
using Kozynax.UI;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Terminal;
using ZXMAK2.Host.WinForms.Lib.Presenters;

namespace ZXMAK2.Host.SdlBackend.Views
{
    public sealed class TerminalObjectSelectorView : IViewImplementation<ObjectSelectorDialog>
    {
        private readonly ITerminal _terminal;
        private ObjectSelectorDialog _ui;

        public TerminalObjectSelectorView(ITerminal terminal)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        }

        public void Init(ObjectSelectorDialog ui)
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
            if (_ui.ItemsList != null)
                presenter.Focus(_ui.ItemsList);

            var closed = false;
            var ignoreEnterUntil = Environment.TickCount + 250;
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

                        var isEnter = ev.Kind == TerminalEventKind.KeyDown
                                      && TerminalKozuiPresenter.MapKey(ev.Key) == KozuiInputKey.Enter;
                        if (isEnter && unchecked(Environment.TickCount - ignoreEnterUntil) < 0)
                            return true;

                        // Real double-click on a row accepts (WinForms list DoubleClick).
                        if (ev.Kind == TerminalEventKind.MouseDown
                            && TryHandleListDoubleClick(presenter, ev))
                            return true;

                        if (TerminalDialogInput.Route(presenter, ev))
                            return false;

                        if (isEnter)
                        {
                            _ui.Accept();
                            return true;
                        }

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

        private bool TryHandleListDoubleClick(TerminalKozuiPresenter presenter, TerminalEvent ev)
        {
            if (_ui?.ItemsList == null || presenter == null || ev.Button != TerminalMouseButton.Left)
                return false;

            var list = _ui.ItemsList;
            var bounds = list.ArrangedBounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return false;

            const int cellW = 8;
            const int cellH = 8;
            const int pad = 1;
            var cellX = ev.X / cellW;
            var cellY = ev.Y / cellH;
            if (cellX < bounds.X || cellX >= bounds.X + bounds.Width
                || cellY < bounds.Y || cellY >= bounds.Y + bounds.Height)
                return false;

            var row = cellY - (bounds.Y + pad);
            var contentH = Math.Max(0, bounds.Height - pad * 2);
            if (row < 0 || row >= contentH || row >= list.Count)
                return false;

            list.SelectedIndex = row;
            presenter.Focus(list);

            // Track double-click via TickCount on the view instance fields.
            var now = Environment.TickCount;
            var isDouble = row == _clickRow
                           && unchecked(now - _clickTick) <= 500;
            if (isDouble)
            {
                _clickTick = 0;
                _clickRow = -1;
                _ui.Accept();
                return true;
            }

            _clickTick = now;
            _clickRow = row;
            return true;
        }

        private int _clickTick;
        private int _clickRow = -1;

        public void Dispose()
        {
        }
    }
}
