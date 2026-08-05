using System;
using System.ComponentModel;
using Kozynax.UI;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Presentation.Interfaces;
using ZXMAK2.Host.Terminal;
namespace ZXMAK2.Host.SdlBackend.Views
{
    /// <summary>
    /// Terminal host for <see cref="TapeSettings"/> Kozui tree (replaces WinForms TapeForm on SDL).
    /// </summary>
    public sealed class TerminalTapeView : ITapeView
    {
        private readonly ITerminal _terminal;
        private TapeSettings _ui;
        private bool _loopActive;
        private bool _closeRequested;

        public TerminalTapeView(ITerminal terminal)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
        }

        public event EventHandler ViewClosed;
        public event CancelEventHandler ViewClosing;

        public void Init(TapeSettings tapeSettings)
        {
            _ui = tapeSettings ?? throw new ArgumentNullException(nameof(tapeSettings));
        }

        public DlgResult ShowDialog(object owner)
        {
            Show(owner as IMainView);
            return DlgResult.OK;
        }

        public void Show(IMainView parent)
        {
            if (_ui == null || !_terminal.IsAvailable || _loopActive)
                return;

            _closeRequested = false;
            _loopActive = true;
            _terminal.PrepareForUiInput();
            var presenter = new TerminalKozuiPresenter(_terminal);
            presenter.Attach(_ui.Root);

            var lastTick = Environment.TickCount;
            try
            {
                while (!_closeRequested)
                {
                    while (_terminal.PollEvent(out var ev))
                    {
                        if (ev.Kind == TerminalEventKind.Quit)
                        {
                            Close();
                            break;
                        }

                        if (TerminalDialogInput.IsEscape(ev))
                        {
                            var args = new CancelEventArgs();
                            ViewClosing?.Invoke(this, args);
                            if (!args.Cancel)
                                Close();
                            // When cancelled, ViewHolder.Hide() sets _closeRequested.
                            break;
                        }

                        TerminalDialogInput.Route(presenter, ev);
                    }

                    if (_closeRequested)
                        break;

                    if (_ui != null && _ui.ProgressTimer.Enabled)
                    {
                        var now = Environment.TickCount;
                        var interval = Math.Max(50, _ui.ProgressTimer.IntervalMs);
                        if (unchecked(now - lastTick) >= interval)
                        {
                            lastTick = now;
                            _ui.ProgressTimer.Tick();
                        }
                    }

                    presenter.MeasureArrangeFromTerminal();
                    presenter.Render();
                    _terminal.Delay(16);
                }
            }
            finally
            {
                _loopActive = false;
                _terminal.EndUiInput();
            }
        }

        public void Hide()
        {
            _closeRequested = true;
        }

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
