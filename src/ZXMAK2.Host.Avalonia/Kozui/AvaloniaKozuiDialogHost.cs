using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Threading;
using Kozui.Abstract;
using Kozui.Interfaces;
using ZXMAK2.Host.AvaloniaBackend.Services;
using ZXMAK2.Host.AvaloniaBackend.Views;
using ZXMAK2.Host.Entities;

namespace ZXMAK2.Host.AvaloniaBackend.Kozui
{
    /// <summary>
    /// Generic <see cref="IViewImplementation{T}"/> for Kozui dialogs: builds a
    /// <see cref="KozuiDialogWindow"/> from the dialog's control tree and runs it modally.
    /// </summary>
    public sealed class AvaloniaKozuiDialogHost<T> : IViewImplementation<T>
        where T : class
    {
        private readonly AvaloniaRuntimeContext _runtime;
        private T _ui;
        private KozuiDialogBinding _binding;

        public AvaloniaKozuiDialogHost(AvaloniaRuntimeContext runtime)
        {
            _runtime = runtime;
        }

        public void Init(T ui)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _binding = new KozuiDialogBinding(ui);
        }

        public DlgResult ShowDialog(object owner)
        {
            if (_binding == null)
                throw new InvalidOperationException("Init must be called before ShowDialog.");
            if (!Dispatcher.UIThread.CheckAccess())
                return Dispatcher.UIThread.Invoke(() => ShowDialog(owner));

            return ModalScope.Run(_runtime, () =>
            {
                var ownerWindow = ResolveOwner(owner);
                var window = new KozuiDialogWindow(_ui, _binding, ownerWindow);
                OpenKozuiWindows.Push(window);
                try
                {
                    if (ownerWindow != null && ownerWindow.IsVisible)
                        AvaloniaRuntimeContext.RunSync(() => window.ShowDialog(ownerWindow));
                    else
                        ShowDetached(window);
                }
                finally
                {
                    OpenKozuiWindows.Pop(window);
                }
                return _binding.DialogResult;
            });
        }

        private Window ResolveOwner(object owner)
        {
            if (owner is Window window)
                return window;
            return OpenKozuiWindows.Top ?? _runtime?.MainWindow;
        }

        private static void ShowDetached(KozuiDialogWindow window)
        {
            var frame = new DispatcherFrame();
            window.Closed += (s, e) => frame.Continue = false;
            window.Show();
            Dispatcher.UIThread.PushFrame(frame);
        }

        public void Dispose()
        {
            _ui = null;
            _binding = null;
        }
    }

    /// <summary>
    /// Innermost open Kozui window. Nested dialogs (MachineSettings → AddDeviceDialog) pass
    /// the host instance as owner, so the real parent window is tracked here. Not inside the
    /// generic host: a static there would be one stack per closed type.
    /// </summary>
    internal static class OpenKozuiWindows
    {
        private static readonly Stack<Window> s_open = new Stack<Window>();

        public static Window Top => s_open.Count > 0 ? s_open.Peek() : null;

        public static void Push(Window window) => s_open.Push(window);

        public static void Pop(Window window)
        {
            if (s_open.Count > 0 && ReferenceEquals(s_open.Peek(), window))
                s_open.Pop();
        }
    }
}
