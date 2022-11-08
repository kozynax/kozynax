using System;

namespace ZXMAK2.Host.WinForms.Lib
{
    public class Timer : KozuiControl
    {
        public event EventHandler OnTick;
        public int IntervalMs { get; set; }
        public void Tick() => OnTick?.Invoke(this, EventArgs.Empty);
    }
}