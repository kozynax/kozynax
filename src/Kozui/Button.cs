using System;

namespace ZXMAK2.Host.WinForms.Lib
{
    public class Button : KozuiControl
    {
        public event EventHandler Clicked;

        public void Click(object sender, EventArgs args)
            => Clicked?.Invoke(sender, args);
    }
}
