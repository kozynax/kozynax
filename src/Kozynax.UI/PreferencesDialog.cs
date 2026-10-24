using System;
using Kozui.Abstract;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Host.WinForms.Lib.Layout;

namespace Kozynax.UI
{
    /// <summary>
    /// SDL preferences. Font Size holds the menu and debugger steps.
    /// </summary>
    [KozuiDialog(CaptureBackdrop = true)]
    public class PreferencesDialog : ViewDescription<PreferencesDialog>
    {
        private readonly ComboBox _menuFont;
        private readonly ComboBox _debuggerFont;

        public event EventHandler CloseRequested;

        public Panel Root { get; }
        public DlgResult DialogResult { get; private set; } = DlgResult.Cancel;

        public int MenuFontSize => UiFontSteps.FromIndex(_menuFont.SelectedIndex);

        public int DebuggerFontSize => UiFontSteps.FromIndex(_debuggerFont.SelectedIndex);

        public PreferencesDialog(int menuFontSize, int debuggerFontSize)
        {
            var title = new Label
            {
                Text = "Preferences",
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            var group = new Label
            {
                Text = "Font Size",
                Margin = new Thickness(0, 1, 0, 0),
            };
            _menuFont = CreateFontCombo(menuFontSize);
            _debuggerFont = CreateFontCombo(debuggerFontSize);

            var rows = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 1,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(1, 1, 0, 0),
            };
            rows.Add(FontRow("Menu", _menuFont));
            rows.Add(FontRow("Debugger", _debuggerFont));

            var ok = new Button { Text = "OK" };
            var cancel = new Button { Text = "Cancel" };
            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 1, 0, 0),
            };
            buttons.Add(ok);
            buttons.Add(cancel);

            var content = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(2),
                MinWidth = 32,
            };
            content.Add(title);
            content.Add(group);
            content.Add(rows);
            content.Add(buttons);

            var frame = new Placeholder
            {
                Content = content,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(3),
                MinWidth = 36,
                MinHeight = 8,
            };

            Root = new Panel();
            Root.Add(frame);

            ok.Clicked += (_, __) => Accept();
            cancel.Clicked += (_, __) => Cancel();
        }

        public void Accept() => Complete(DlgResult.OK);

        public void Cancel() => Complete(DlgResult.Cancel);

        private void Complete(DlgResult result)
        {
            DialogResult = result;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private static ComboBox CreateFontCombo(int fontSize)
        {
            var combo = new ComboBox
            {
                Dock = Dock.Fill,
                Margin = new Thickness(1, 0, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                MinWidth = 10,
            };
            combo.SetItems(UiFontSteps.Names);
            combo.SelectedIndex = UiFontSteps.ToIndex(fontSize);
            return combo;
        }

        private static DockPanel FontRow(string caption, ComboBox combo)
        {
            var label = new Label
            {
                Text = caption,
                Dock = Dock.Left,
                MinWidth = 10,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var row = new DockPanel
            {
                MinHeight = 1,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            row.Add(label);
            row.Add(combo);
            return row;
        }
    }
}
