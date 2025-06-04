using System;

namespace ZXMAK2.Host.WinForms.Lib
{
    public class TrackBar
    {
        public event EventHandler ValueChanged;
        public int Minimum { get; set; }
        public int Maximum { get; set; }
        public int Value { get; set; }
    }
}