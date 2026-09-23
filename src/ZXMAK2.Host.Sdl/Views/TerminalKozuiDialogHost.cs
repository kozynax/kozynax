using System;
using Kozui.Abstract;
using Kozui.Interfaces;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.SdlBackend;
using ZXMAK2.Host.Terminal;
using ZXMAK2.Host.WinForms.Lib.Presenters;

namespace ZXMAK2.Host.SdlBackend.Views
{
    /// <summary>
    /// Generic Terminal host for any Kozui dialog that exposes <c>Root</c> + <c>CloseRequested</c>
    /// (typically a <see cref="ViewDescription{T}"/>). Replaces per-dialog Terminal*View classes.
    /// Optional <see cref="Session"/> covers debugger-style custom input.
    /// </summary>
    public sealed class TerminalKozuiDialogHost<T> : IViewImplementation<T>
        where T : class
    {
        private readonly ITerminal _terminal;
        private readonly SdlRuntimeContext _runtime;
        private T _ui;
        private KozuiDialogBinding _binding;

        public TerminalKozuiDialogHost(ITerminal terminal, SdlRuntimeContext runtime)
        {
            _terminal = terminal ?? throw new ArgumentNullException(nameof(terminal));
            _runtime = runtime;
        }

        /// <summary>Optional debugger/tool session hooks (input, fit, idle).</summary>
        public IKozuiTerminalSession Session { get; set; }

        public void Init(T ui)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _binding = new KozuiDialogBinding(ui);
        }

        public DlgResult ShowDialog(object owner)
        {
            if (_ui == null || _binding == null || !_terminal.IsAvailable)
                return DlgResult.Cancel;

            var options = _binding.Options;
            var session = Session;
            _terminal.PrepareForUiInput();
            if (options.CaptureBackdrop)
                _terminal.CaptureBackdrop();

            SdlMenuImagePainter imagePainter = null;
            if (options.RequireImagePainter && _runtime != null)
                imagePainter = _runtime.CreateMenuImagePainter();

            var presenter = new TerminalKozuiPresenter(_terminal, _terminal.UiScale)
            {
                ImagePainter = imagePainter,
            };
            presenter.Attach(_binding.Root);

            var closed = false;
            EventHandler onClose = (_, __) => closed = true;
            _binding.AddCloseHandler(onClose);

            var ignoreEnterUntil = Environment.TickCount + 250;
            var lastTick = Environment.TickCount;

            Action previousIdle = null;
            var sdlView = owner as SdlMainView;
            if (session != null)
            {
                previousIdle = TerminalUiSession.IdlePump;
                TerminalUiSession.IdlePump = () =>
                {
                    previousIdle?.Invoke();
                    sdlView?.PumpUiCallbacks();
                };
                session.OnSessionStart(owner, presenter);
            }

            try
            {
                TerminalUiSession.Run(
                    _terminal,
                    presenter,
                    () => closed || (session != null && session.ShouldClose),
                    ev =>
                    {
                        if (session != null && session.TryHandleEvent(presenter, ev))
                            return true;

                        if (ev.Kind == TerminalEventKind.Quit)
                        {
                            closed = true;
                            return true;
                        }

                        if (TerminalDialogInput.IsEscape(ev))
                        {
                            TryCancel(_ui);
                            closed = true;
                            return true;
                        }

                        var isEnter = ev.Kind == TerminalEventKind.KeyDown
                                      && TerminalKozuiPresenter.MapKey(ev.Key) == KozuiInputKey.Enter;
                        if (isEnter && unchecked(Environment.TickCount - ignoreEnterUntil) < 0)
                            return true;

                        if (TerminalDialogInput.Route(presenter, ev))
                            return false;

                        if (isEnter)
                        {
                            TryAccept(_ui);
                            return true;
                        }

                        return false;
                    },
                    beforeRender: () =>
                    {
                        session?.BeforeRender(presenter);

                        var timer = _binding.UpdateTimer;
                        if (timer == null || !timer.Enabled)
                            return;
                        var now = Environment.TickCount;
                        var interval = Math.Max(50, timer.IntervalMs);
                        if (unchecked(now - lastTick) >= interval)
                        {
                            lastTick = now;
                            timer.Tick();
                        }
                    });

                return _binding.DialogResult;
            }
            finally
            {
                if (session != null)
                {
                    try
                    {
                        session.OnSessionEnd();
                    }
                    finally
                    {
                        TerminalUiSession.IdlePump = previousIdle;
                    }
                }

                _binding.RemoveCloseHandler(onClose);
                imagePainter?.Dispose();
                if (options.CaptureBackdrop)
                    _terminal.ReleaseBackdrop();
                _terminal.EndUiInput();
            }
        }

        private static void TryAccept(object ui)
        {
            var method = ui?.GetType().GetMethod("Accept", Type.EmptyTypes);
            method?.Invoke(ui, null);
        }

        private static void TryCancel(object ui)
        {
            if (ui == null)
                return;
            var type = ui.GetType();
            var method = type.GetMethod("Cancel", Type.EmptyTypes)
                         ?? type.GetMethod("RequestCancel", Type.EmptyTypes);
            if (method != null)
                method.Invoke(ui, null);
            else
                TryAccept(ui); // close-only dialogs
        }

        public void Dispose()
        {
            Session = null;
            _ui = null;
            _binding = null;
        }
    }
}
