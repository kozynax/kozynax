using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using ZXMAK2.Host.Entities;
using Key = Avalonia.Input.Key;

namespace ZXMAK2.Host.AvaloniaBackend.Views
{
    /// <summary>
    /// Minimal modal message box: text + a <see cref="DlgButtonSet"/> button row.
    /// </summary>
    public sealed class MessageWindow : Window
    {
        private DlgResult _result = DlgResult.None;
        private readonly DlgResult _cancelResult;

        private MessageWindow(string caption, string message, DlgButtonSet buttonSet)
        {
            Title = caption ?? string.Empty;
            CanResize = false;
            SizeToContent = SizeToContent.WidthAndHeight;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ShowInTaskbar = false;
            MinWidth = 320;
            MaxWidth = 720;

            var buttons = Buttons(buttonSet);
            _cancelResult = buttons.Count > 0 ? buttons[buttons.Count - 1].Result : DlgResult.Cancel;

            var text = new SelectableTextBlock
            {
                Text = message ?? string.Empty,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 480,
                Margin = new Thickness(16, 16, 16, 8),
            };

            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 8,
                Margin = new Thickness(16, 8, 16, 16),
            };
            Button first = null;
            foreach (var b in buttons)
            {
                var result = b.Result;
                var button = new Button { Content = b.Text, MinWidth = 80, HorizontalContentAlignment = HorizontalAlignment.Center };
                button.Click += (s, e) => CloseWith(result);
                if (first == null)
                {
                    first = button;
                    button.IsDefault = true;
                }
                row.Children.Add(button);
            }

            Content = new StackPanel
            {
                Children = { new ScrollViewer { Content = text }, row },
            };

            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape)
                {
                    CloseWith(_cancelResult);
                    e.Handled = true;
                }
            };
            Opened += (s, e) => first?.Focus();
        }

        private void CloseWith(DlgResult result)
        {
            _result = result;
            Close();
        }

        public static DlgResult Show(Window owner, string caption, string message, DlgButtonSet buttonSet)
        {
            try
            {
                return ShowCore(owner, caption, message, buttonSet);
            }
            catch (Exception ex)
            {
                // No windowing platform (e.g. startup failed before Avalonia init): keep the text.
                Logger.Error(ex);
                Console.Error.WriteLine("{0}: {1}", caption, message);
                return DlgResult.Cancel;
            }
        }

        private static DlgResult ShowCore(Window owner, string caption, string message, DlgButtonSet buttonSet)
        {
            return AvaloniaRuntimeContext.RunSync(async () =>
            {
                var window = new MessageWindow(caption, message, buttonSet);
                if (owner != null && owner.IsVisible)
                    await window.ShowDialog(owner);
                else
                {
                    var tcs = new TaskCompletionSource<bool>();
                    window.Closed += (s, e) => tcs.TrySetResult(true);
                    window.Show();
                    await tcs.Task;
                }
                return window._result == DlgResult.None ? window._cancelResult : window._result;
            });
        }

        private static List<(string Text, DlgResult Result)> Buttons(DlgButtonSet set)
        {
            switch (set)
            {
                case DlgButtonSet.OKCancel:
                    return new List<(string, DlgResult)> { ("OK", DlgResult.OK), ("Cancel", DlgResult.Cancel) };
                case DlgButtonSet.AbortRetryIgnore:
                    return new List<(string, DlgResult)> { ("Abort", DlgResult.Abort), ("Retry", DlgResult.Retry), ("Ignore", DlgResult.Ignore) };
                case DlgButtonSet.YesNoCancel:
                    return new List<(string, DlgResult)> { ("Yes", DlgResult.Yes), ("No", DlgResult.No), ("Cancel", DlgResult.Cancel) };
                case DlgButtonSet.YesNo:
                    return new List<(string, DlgResult)> { ("Yes", DlgResult.Yes), ("No", DlgResult.No) };
                case DlgButtonSet.RetryCancel:
                    return new List<(string, DlgResult)> { ("Retry", DlgResult.Retry), ("Cancel", DlgResult.Cancel) };
                default:
                    return new List<(string, DlgResult)> { ("OK", DlgResult.OK) };
            }
        }
    }
}
