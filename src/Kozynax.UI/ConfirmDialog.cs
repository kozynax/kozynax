using System;
using Kozui.Abstract;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Host.WinForms.Lib.Layout;

namespace Kozynax.UI
{
    /// <summary>
    /// Small Kozui-tree dialog (Label + OK/Cancel) used to prove portable layout
    /// before migrating MachineSettings.
    /// </summary>
    public class ConfirmDialog : ViewDescription<ConfirmDialog>
    {
        public event EventHandler CloseRequested;

        public Panel Root { get; }
        public Label TitleLabel { get; }
        public Label MessageLabel { get; }
        public Button OkButton { get; }
        public Button CancelButton { get; }
        public DlgResult DialogResult { get; private set; } = DlgResult.Cancel;

        public ConfirmDialog(string message, string caption = null, bool showCancel = true)
            : this(message, caption, showCancel, DlgResult.OK, DlgResult.Cancel, "OK", "Cancel")
        {
        }

        private ConfirmDialog(
            string message,
            string caption,
            bool showCancel,
            DlgResult acceptResult,
            DlgResult rejectResult,
            string acceptText,
            string rejectText)
        {
            TitleLabel = new Label
            {
                Text = string.IsNullOrEmpty(caption) ? "Confirm" : caption,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            MessageLabel = new Label
            {
                Text = message ?? string.Empty,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 1, 0, 1),
            };
            OkButton = new Button { Text = acceptText };
            CancelButton = new Button
            {
                Text = rejectText,
                Visible = showCancel,
            };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            buttons.Add(OkButton);
            if (showCancel)
                buttons.Add(CancelButton);

            var content = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 1,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2),
            };
            content.Add(TitleLabel);
            content.Add(MessageLabel);
            content.Add(buttons);

            Root = new Panel();
            Root.Add(content);

            OkButton.Clicked += (_, __) => Complete(acceptResult);
            CancelButton.Clicked += (_, __) => Complete(rejectResult);
        }

        public static ConfirmDialog ForButtonSet(string message, string caption, DlgButtonSet buttonSet)
        {
            switch (buttonSet)
            {
                case DlgButtonSet.OK:
                    return new ConfirmDialog(message, caption, showCancel: false);
                case DlgButtonSet.YesNo:
                    return new ConfirmDialog(
                        message,
                        caption,
                        showCancel: true,
                        DlgResult.Yes,
                        DlgResult.No,
                        "Yes",
                        "No");
                default:
                    return new ConfirmDialog(message, caption, showCancel: true);
            }
        }

        public void Cancel() => Complete(DlgResult.Cancel);

        private void Complete(DlgResult result)
        {
            DialogResult = result;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }
    }
}
