using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using Kozui.Abstract;
using K = ZXMAK2.Host.WinForms.Lib;
using KL = ZXMAK2.Host.WinForms.Lib.Layout;
using Key = Avalonia.Input.Key;

namespace ZXMAK2.Host.AvaloniaBackend.Kozui
{
    /// <summary>
    /// A modal window that presents one Kozui dialog tree (<see cref="KozuiDialogBinding"/>).
    /// Keyboard semantics follow the Terminal presenter: Esc cancels, Enter accepts
    /// unless a focused control consumed it, and the dialog's UpdateTimer is driven
    /// from the UI thread.
    /// </summary>
    internal sealed class KozuiDialogWindow : Window
    {
        private readonly object _ui;
        private readonly KozuiDialogBinding _binding;
        private readonly KozuiTreeBuilder _builder = new KozuiTreeBuilder();
        private readonly EventHandler _closeHandler;
        private DispatcherTimer _timer;
        private bool _closing;

        public KozuiDialogWindow(object ui, KozuiDialogBinding binding, Window owner)
        {
            _ui = ui ?? throw new ArgumentNullException(nameof(ui));
            _binding = binding ?? throw new ArgumentNullException(nameof(binding));

            Title = ResolveTitle(ui);
            Icon = owner?.Icon;
            CanResize = false;
            ShowInTaskbar = false;
            WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen;
            SizeToContent = SizeToContent.WidthAndHeight;
            FontFamily = new FontFamily("Noto Sans Mono,DejaVu Sans Mono,Consolas,Menlo,monospace");
            FontSize = 13;

            var root = binding.Root;
            var stretch = IsStretchDialog(root);
            _builder.ConstrainStretchLists = !stretch;
            var content = _builder.Build(root);
            content.Margin = new Thickness(KozuiTreeBuilder.CellW, KozuiTreeBuilder.CellH);
            Content = content;

            if (stretch)
            {
                CanResize = true;
                SizeToContent = SizeToContent.Manual;
                var ownerW = owner?.Bounds.Width ?? 0;
                var ownerH = owner?.Bounds.Height ?? 0;
                Width = Math.Max(640, ownerW * 0.8);
                Height = Math.Max(480, ownerH * 0.8);
                MinWidth = 480;
                MinHeight = 360;
            }

            _closeHandler = (s, e) => RequestClose();
            _binding.AddCloseHandler(_closeHandler);

            AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
            KeyDown += OnKeyDown;
            Opened += OnOpened;
            Closing += OnClosing;
            Closed += OnClosed;
        }

        private static bool IsStretchDialog(K.KozuiControl root)
        {
            // Dialogs that fill the Terminal screen expose a Stretch/Stretch Placeholder near the root.
            if (root == null)
                return false;
            if (root.HorizontalAlignment == KL.HorizontalAlignment.Stretch &&
                root.VerticalAlignment == KL.VerticalAlignment.Stretch &&
                ContainsStretchHost(root, 0))
                return true;
            return false;
        }

        private static bool ContainsStretchHost(K.KozuiControl control, int depth)
        {
            if (depth > 3)
                return false;
            if (control is K.Placeholder || control is K.ListView)
            {
                return control.HorizontalAlignment == KL.HorizontalAlignment.Stretch &&
                       control.VerticalAlignment == KL.VerticalAlignment.Stretch &&
                       control.MinHeight == 0;
            }
            if (control is K.Panel panel)
            {
                foreach (var child in panel.Children)
                {
                    if (ContainsStretchHost(child, depth + 1))
                        return true;
                }
            }
            return false;
        }

        private static string ResolveTitle(object ui)
        {
            var label = ui.GetType().GetProperty("TitleLabel")?.GetValue(ui) as K.Label;
            var text = label?.Text?.Trim();
            if (!string.IsNullOrEmpty(text))
                return text;
            var name = ui.GetType().Name;
            if (name.EndsWith("Dialog", StringComparison.Ordinal))
                name = name.Substring(0, name.Length - "Dialog".Length);
            var sb = new System.Text.StringBuilder(name.Length + 4);
            for (var i = 0; i < name.Length; i++)
            {
                if (i > 0 && char.IsUpper(name[i]) && !char.IsUpper(name[i - 1]))
                    sb.Append(' ');
                sb.Append(name[i]);
            }
            return sb.ToString();
        }

        #region Lifecycle

        private void OnOpened(object sender, EventArgs e)
        {
            FocusInitial();

            var timer = _binding.UpdateTimer;
            if (timer != null)
            {
                _timer = new DispatcherTimer
                {
                    Interval = TimeSpan.FromMilliseconds(Math.Max(50, timer.IntervalMs)),
                };
                _timer.Tick += (s, a) =>
                {
                    if (timer.Enabled)
                        timer.Tick();
                };
                _timer.Start();
            }
        }

        private void FocusInitial()
        {
            Control target = null;
            foreach (var control in _builder.Focusables)
            {
                if (control is TextBox && control.IsEffectivelyVisible && control.IsEffectivelyEnabled)
                {
                    target = control;
                    break;
                }
            }
            if (target == null)
            {
                foreach (var control in _builder.Focusables)
                {
                    if (control is ListBox && control.IsEffectivelyVisible && control.IsEffectivelyEnabled)
                    {
                        target = control;
                        break;
                    }
                }
            }
            if (target == null)
            {
                foreach (var control in _builder.Focusables)
                {
                    if (control.IsEffectivelyVisible && control.IsEffectivelyEnabled)
                    {
                        target = control;
                        break;
                    }
                }
            }
            if (target != null)
                Dispatcher.UIThread.Post(() => target.Focus(), DispatcherPriority.Input);
            else
                Focus();
        }

        private void RequestClose()
        {
            if (_closing)
                return;
            _closing = true;
            if (Dispatcher.UIThread.CheckAccess())
                Close();
            else
                Dispatcher.UIThread.Post(Close);
        }

        private void OnClosing(object sender, WindowClosingEventArgs e)
        {
            if (_closing)
                return;
            // Window-manager close: route through the dialog so it sets DialogResult.
            e.Cancel = true;
            TryCancel();
        }

        private void OnClosed(object sender, EventArgs e)
        {
            _timer?.Stop();
            _timer = null;
            _binding.RemoveCloseHandler(_closeHandler);
            _builder.Dispose();
        }

        #endregion

        #region Keyboard

        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Escape)
                return;
            if (FocusManager?.GetFocusedElement() is ComboBox combo && combo.IsDropDownOpen)
            {
                combo.IsDropDownOpen = false;
                e.Handled = true;
                return;
            }
            e.Handled = true;
            TryCancel();
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Handled || e.Key != Key.Enter)
                return;
            var focused = FocusManager?.GetFocusedElement();
            if (focused is Button || focused is ComboBox || focused is ListBox)
                return; // the control acted (or chose not to); keep Terminal parity
            if (focused is TextBox textBox && textBox.AcceptsReturn)
                return;
            e.Handled = true;
            TryAccept();
        }

        private void TryCancel()
        {
            if (!Invoke("Cancel") && !Invoke("RequestCancel") && !Invoke("Accept"))
                RequestClose();
        }

        private void TryAccept()
        {
            Invoke("Accept");
        }

        private bool Invoke(string method)
        {
            var info = _ui.GetType().GetMethod(method, Type.EmptyTypes);
            if (info == null)
                return false;
            try
            {
                info.Invoke(_ui, null);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return true;
        }

        #endregion
    }
}
