using System;

namespace ZXMAK2.Host.WinForms.Lib
{
    public class Timer : KozuiControl
    {
        private int _intervalMs;

        public event EventHandler OnTick;

        public int IntervalMs
        {
            get => _intervalMs;
            set => SetProperty(ref _intervalMs, value);
        }

        public void Tick() => OnTick?.Invoke(this, EventArgs.Empty);
    }
}
