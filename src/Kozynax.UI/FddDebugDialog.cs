using System;
using ZXMAK2.Hardware.Circuits.Fdd;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Host.WinForms.Lib.Layout;
using Button = ZXMAK2.Host.WinForms.Lib.Button;
using Timer = ZXMAK2.Host.WinForms.Lib.Timer;

namespace Kozynax.UI
{
    /// <summary>
    /// Kozui WD1793 / Beta Disk debug tool window (live <see cref="Wd1793.DumpState"/>).
    /// </summary>
    public sealed class FddDebugDialog
    {
        private const string Missing = "Beta Disk interface not found";

        private readonly Wd1793 _wd1793;
        private string _lastDump;

        public event EventHandler CloseRequested;

        public Panel Root { get; }
        public Label TitleLabel { get; }
        public ListView<string> StateList { get; }
        public Timer UpdateTimer { get; }
        public Button CloseButton { get; }

        public FddDebugDialog(Wd1793 wd1793)
        {
            _wd1793 = wd1793;

            TitleLabel = new Label
            {
                Text = "WD1793",
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            StateList = new ListView<string>
            {
                ItemTextSelector = line => line ?? string.Empty,
                Dock = Dock.Fill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                MinWidth = 28,
                MinHeight = 16,
            };

            UpdateTimer = new Timer { IntervalMs = 300, Enabled = true };
            UpdateTimer.OnTick += (_, __) => Refresh();

            CloseButton = new Button { Text = "Close" };
            CloseButton.Clicked += (_, __) => CloseRequested?.Invoke(this, EventArgs.Empty);

            Root = BuildTree();
            Refresh();
        }

        public void Refresh()
        {
            var dump = _wd1793 != null ? _wd1793.DumpState() : Missing;
            if (dump == _lastDump)
                return;

            _lastDump = dump;
            var lines = dump.Replace("\r\n", "\n").Split('\n');
            var selected = StateList.SelectedIndex;
            StateList.Reset(lines);
            if (selected >= 0 && selected < StateList.Count)
                StateList.SelectedIndex = selected;
        }

        public void Close()
        {
            UpdateTimer.Enabled = false;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private Panel BuildTree()
        {
            TitleLabel.Dock = Dock.Top;
            TitleLabel.Margin = new Thickness(0, 0, 0, 1);

            CloseButton.Dock = Dock.Bottom;
            CloseButton.HorizontalAlignment = HorizontalAlignment.Center;
            CloseButton.Margin = new Thickness(0, 1, 0, 0);

            var content = new DockPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(2),
                MinWidth = 30,
                MinHeight = 20,
            };
            content.Add(TitleLabel);
            content.Add(CloseButton);
            content.Add(StateList);

            var frame = new Placeholder
            {
                Content = content,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2),
                MinWidth = 32,
                MinHeight = 22,
            };

            var root = new Panel();
            root.Add(frame);
            return root;
        }
    }
}
