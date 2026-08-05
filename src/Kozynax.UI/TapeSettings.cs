using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Kozui.Abstract;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Host.WinForms.Lib.Layout;
using ZXMAK2.Model.Tape.Interfaces;
using Button = ZXMAK2.Host.WinForms.Lib.Button;
using ProgressBar = ZXMAK2.Host.WinForms.Lib.ProgressBar;
using Timer = ZXMAK2.Host.WinForms.Lib.Timer;

namespace Kozynax.UI
{
    public class TapeSettings : ViewDescription<TapeSettings>, INotifyPropertyChanged
    {
        private readonly ITapeDevice _tape;
        private bool _isTapePlaying;

        public event PropertyChangedEventHandler PropertyChanged;

        public Panel Root { get; }
        public Button Rewind { get; }
        public Button Prev { get; }
        public Button Play { get; }
        public Button Next { get; }
        public ProgressBar ProgressBar { get; }
        public Timer ProgressTimer { get; }
        public CheckBox UseTraps { get; }
        public CheckBox UseAutoPlay { get; }
        public ListView<ITapeBlock> Blocks { get; }

        public TapeSettings(ITapeDevice tape)
        {
            _tape = tape;
            _tape.TapeStateChanged += Tape_TapeStateChanged;

            Rewind = new Button { Text = "<<" };
            Rewind.Clicked += (sender, args) => _tape.Rewind();

            Prev = new Button { Text = "<" };
            Prev.Clicked += (sender, args) => _tape.CurrentBlock--;

            Play = new Button { Text = "Play" };
            Play.Clicked += Play_Clicked;

            Next = new Button { Text = ">" };
            Next.Clicked += (sender, args) => _tape.CurrentBlock++;

            ProgressTimer = new Timer();
            ProgressTimer.IntervalMs = 200;
            ProgressTimer.Enabled = true;
            ProgressTimer.OnTick += ProgressTimer_OnTick;

            UseTraps = new CheckBox { Text = "Traps" };
            UseTraps.CheckedStateChanged += (s, e) => _tape.UseTraps = UseTraps.Checked;

            UseAutoPlay = new CheckBox { Text = "AutoPlay" };
            UseAutoPlay.CheckedStateChanged += (s, e) =>
            {
                _tape.UseAutoPlay = UseAutoPlay.Checked;
                Play.Enabled = !UseAutoPlay.Checked;
            };

            Blocks = new ListView<ITapeBlock>
            {
                ItemTextSelector = b => b?.Description ?? string.Empty,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Dock = Dock.Fill,
            };
            Blocks.SelectedIndexChanged += Blocks_SelectedIndexChanged;
            Blocks.ItemActivated += (s, e) => Play.Click(Play, EventArgs.Empty);

            ProgressBar = new ProgressBar
            {
                MinWidth = 16,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };

            Root = BuildTree();
            Tape_TapeStateChanged(_tape, EventArgs.Empty);
        }

        private Panel BuildTree()
        {
            var title = new Label
            {
                Text = "Tape",
                HorizontalAlignment = HorizontalAlignment.Left,
            };

            Rewind.Dock = Dock.Left;
            Prev.Dock = Dock.Left;
            Play.Dock = Dock.Left;
            Next.Dock = Dock.Left;
            ProgressBar.Dock = Dock.Fill;
            ProgressBar.Margin = new Thickness(1, 0, 0, 0);

            var toolbar = new DockPanel
            {
                Dock = Dock.Top,
                Margin = new Thickness(0, 0, 0, 1),
                MinHeight = 1,
            };
            toolbar.Add(Rewind);
            toolbar.Add(Prev);
            toolbar.Add(Play);
            toolbar.Add(Next);
            toolbar.Add(ProgressBar);

            var options = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                Dock = Dock.Top,
                Margin = new Thickness(0, 0, 0, 1),
            };
            options.Add(UseTraps);
            options.Add(UseAutoPlay);

            var help = new Label
            {
                Text = "Tab focus  Enter act  Up/Dn list  Esc close",
                Dock = Dock.Bottom,
                Margin = new Thickness(0, 1, 0, 0),
            };

            var root = new DockPanel
            {
                Margin = new Thickness(1),
            };
            root.Add(title);
            title.Dock = Dock.Top;
            root.Add(toolbar);
            root.Add(options);
            root.Add(help);
            root.Add(Blocks);
            return root;
        }

        public bool IsTapePlaying
        {
            get => _isTapePlaying;
            private set
            {
                if (_isTapePlaying == value)
                    return;
                _isTapePlaying = value;
                Play.Text = value ? "Stop" : "Play";
                OnPropertyChanged();
            }
        }

        private void Play_Clicked(object sender, EventArgs e)
        {
            if (_tape.IsPlay)
                _tape.Stop();
            else
                _tape.Play();
        }

        private void Tape_TapeStateChanged(object sender, EventArgs e)
        {
            IsTapePlaying = _tape.IsPlay;

            if (_tape.Blocks.Count <= 0)
            {
                Rewind.Enabled =
                    Prev.Enabled =
                        Play.Enabled =
                            Next.Enabled = false;
                Blocks.SelectedIndex = -1;
            }
            else
            {
                Next.Enabled = Prev.Enabled = !_tape.IsPlay;
                Rewind.Enabled = Play.Enabled = true;
                if (!Blocks.SequenceEqual(_tape.Blocks))
                    Blocks.Reset(_tape.Blocks);
                Blocks.SelectedIndex = _tape.CurrentBlock;
            }
            Blocks.Enabled = !_tape.IsPlay;
            UseTraps.Checked = _tape.UseTraps;
            UseAutoPlay.Checked = _tape.UseAutoPlay;
            Play.Enabled = !UseAutoPlay.Checked;
        }

        private void ProgressTimer_OnTick(object sender, EventArgs e)
        {
            ProgressBar.Minimum = 0;

            int blockCount = _tape.Blocks.Count;
            int curBlock, position, maximum;
            do
            {
                curBlock = _tape.CurrentBlock;
                if (curBlock >= 0 && curBlock < blockCount)
                {
                    maximum = _tape.Blocks[curBlock].Count;
                    position = _tape.Position;
                }
                else
                {
                    maximum = 65535;
                    position = 0;
                }
            } while (position > maximum);

            ProgressBar.Maximum = maximum;
            ProgressBar.Value = position;
        }

        public void Close()
        {
            _tape.TapeStateChanged -= Tape_TapeStateChanged;
        }

        private void Blocks_SelectedIndexChanged(object sender, int index)
        {
            _tape.CurrentBlock = index;
        }

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
