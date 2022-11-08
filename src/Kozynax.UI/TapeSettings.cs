using System;
using System.Collections.Generic;
using System.Linq;
using Kozui.Abstract;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Model.Tape.Interfaces;
using ZXMAK2.Resources;
using Button = ZXMAK2.Host.WinForms.Lib.Button;
using ProgressBar = ZXMAK2.Host.WinForms.Lib.ProgressBar;
using Timer = ZXMAK2.Host.WinForms.Lib.Timer;

namespace Kozynax.UI
{
    public class TapeSettings : ViewDescription<TapeSettings>
    {
        public event EventHandler Redraw;
        
        private readonly ITapeDevice _tape;
        
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
            
            Rewind = new Button();
            Rewind.Clicked += (sender, args) => _tape.Rewind();
            
            Prev = new Button(); 
            Prev.Clicked += (sender, args) => _tape.CurrentBlock--;

            Play = new Button();
            Play.Clicked += Play_Clicked;
            
            Next = new Button(); 
            Next.Clicked += (sender, args) => _tape.CurrentBlock++;

            ProgressTimer = new Timer();
            ProgressTimer.IntervalMs = 200;
            ProgressTimer.OnTick += ProgressTimer_OnTick;

            UseTraps = new CheckBox();
            UseTraps.CheckedStateChanged += (s, e) => _tape.UseTraps = UseTraps.Checked;
            
            UseAutoPlay = new CheckBox();
            UseAutoPlay.CheckedStateChanged += (s, e) =>
            {
                _tape.UseAutoPlay = UseAutoPlay.Checked;
                Play.Enabled = !UseAutoPlay.Checked;
            };

            Blocks = new ListView<ITapeBlock>();
            Blocks.SelectedIndexChanged += Blocks_SelectedIndexChanged;

            ProgressBar = new ProgressBar();
        }

        public bool IsTapePlaying => _tape.IsPlay;
        
        private void Play_Clicked(object sender, EventArgs e)
        {
            if (_tape.IsPlay)
                _tape.Stop();
            else
                _tape.Play();
        }
        
        private void Tape_TapeStateChanged(object sender, EventArgs e)
        {
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
                if (!Enumerable.SequenceEqual(Blocks.List, _tape.Blocks))
                {
                    Blocks.List.Clear();
                    foreach (var tb in _tape.Blocks)
                        Blocks.List.Add(tb);
                }
                Blocks.SelectedIndex = _tape.CurrentBlock;
            }
            Blocks.Enabled = !_tape.IsPlay;
            UseTraps.Checked = _tape.UseTraps;
            UseAutoPlay.Checked = _tape.UseAutoPlay;
            Play.Enabled = !UseAutoPlay.Checked;
            
            Redraw?.Invoke(sender, e);
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
            
            Redraw?.Invoke(this, e);
        }

        public void Close()
        {
            _tape.TapeStateChanged -= new EventHandler(Tape_TapeStateChanged);
        }
        
        private void Blocks_SelectedIndexChanged(object sender, int index)
        {
            _tape.CurrentBlock = index;
        }

    }
}