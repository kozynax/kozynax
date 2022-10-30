using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZXMAK2.Host.WinForms.Lib
{
    public class Button : KozuiControl
    {
        public event EventHandler Clicked;
        public void Click(object sender, EventArgs args)
            => Clicked?.Invoke(sender, args);
    }
}
