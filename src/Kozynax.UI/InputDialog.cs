using System;
using Kozui.Abstract;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Host.WinForms.Lib.Layout;

namespace Kozynax.UI
{
    /// <summary>
    /// Kozui text/value prompt (WinForms InputBox equivalent).
    /// Root fills the screen; <see cref="Frame"/> is a centered bordered card.
    /// </summary>
    public class InputDialog : ViewDescription<InputDialog>
    {
        public event EventHandler CloseRequested;

        public Panel Root { get; }
        public Placeholder Frame { get; }
        public Label TitleLabel { get; }
        public Label PromptLabel { get; }
        public TextBox ValueBox { get; }
        public Label ErrorLabel { get; }
        public Button OkButton { get; }
        public Button CancelButton { get; }
        public DlgResult DialogResult { get; private set; } = DlgResult.Cancel;

        public string Value
        {
            get => ValueBox.Text ?? string.Empty;
            set => ValueBox.Text = value ?? string.Empty;
        }

        public InputDialog(string prompt, string caption = null)
        {
            TitleLabel = new Label
            {
                Text = string.IsNullOrEmpty(caption) ? "Input" : caption,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            PromptLabel = new Label
            {
                Text = prompt ?? string.Empty,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 1, 0, 0),
            };
            ValueBox = new TextBox
            {
                MinWidth = 28,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 1, 0, 0),
            };
            ErrorLabel = new Label
            {
                Text = string.Empty,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 1, 0, 0),
            };
            OkButton = new Button { Text = "OK" };
            CancelButton = new Button { Text = "Cancel" };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 1, 0, 0),
            };
            buttons.Add(OkButton);
            buttons.Add(CancelButton);

            var content = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(2),
                MinWidth = 30,
            };
            content.Add(TitleLabel);
            content.Add(PromptLabel);
            content.Add(ValueBox);
            content.Add(ErrorLabel);
            content.Add(buttons);

            Frame = new Placeholder
            {
                Content = content,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(3),
                MinWidth = 34,
                MinHeight = 10,
            };

            // Full-screen host; only the centered frame paints chrome.
            Root = new Panel();
            Root.Add(Frame);

            OkButton.Clicked += (_, __) => Complete(DlgResult.OK);
            CancelButton.Clicked += (_, __) => Complete(DlgResult.Cancel);
        }

        public void SetError(string message)
            => ErrorLabel.Text = message ?? string.Empty;

        public void Cancel() => Complete(DlgResult.Cancel);

        public static bool Query(string caption, string prompt, ref string value)
        {
            var dialog = new InputDialog(prompt, caption);
            dialog.Value = value ?? string.Empty;
            if (dialog.ShowDialog(null) != DlgResult.OK)
                return false;
            value = dialog.Value;
            return true;
        }

        public static bool QueryValue(
            string caption,
            string prompt,
            string format,
            ref int value,
            int min,
            int max)
        {
            var text = string.Format(format ?? "{0}", value);
            string error = null;
            while (true)
            {
                var dialog = new InputDialog(prompt, caption);
                dialog.Value = text;
                if (!string.IsNullOrEmpty(error))
                    dialog.SetError(error);

                if (dialog.ShowDialog(null) != DlgResult.OK)
                    return false;

                text = dialog.Value ?? string.Empty;
                if (!TryParseInt(text, format, out var parsed))
                {
                    error = "Numeric value required";
                    continue;
                }

                if (parsed < min || parsed > max)
                {
                    error = string.Format("Range: {0}...{1}", min, max);
                    continue;
                }

                value = parsed;
                return true;
            }
        }

        private void Complete(DlgResult result)
        {
            DialogResult = result;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private static bool TryParseInt(string input, string format, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            var s = input.Trim();
            // "#{0:X2}" / "{0:X4}" prompts: bare hex digits (e.g. FF) are accepted.
            var preferHex = !string.IsNullOrEmpty(format)
                            && format.IndexOf(":X", StringComparison.OrdinalIgnoreCase) >= 0;
            try
            {
                if (s.Length > 0 && s[0] == '#')
                {
                    value = Convert.ToInt32(s.Substring(1), 16);
                    return true;
                }
                if (s.Length > 1 && s[0] == '0' && (s[1] == 'x' || s[1] == 'X'))
                {
                    value = Convert.ToInt32(s.Substring(2), 16);
                    return true;
                }
                if (preferHex && IsHexDigits(s))
                {
                    value = Convert.ToInt32(s, 16);
                    return true;
                }
                value = Convert.ToInt32(s, 10);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static bool IsHexDigits(string s)
        {
            if (string.IsNullOrEmpty(s))
                return false;
            for (var i = 0; i < s.Length; i++)
            {
                var c = s[i];
                var hex = (c >= '0' && c <= '9')
                          || (c >= 'a' && c <= 'f')
                          || (c >= 'A' && c <= 'F');
                if (!hex)
                    return false;
            }
            return true;
        }
    }
}
