using System.Windows.Forms;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Hardware;
using ZXMAK2.Host.Interfaces;


namespace ZXMAK2.Host.WinForms.Views.Configuration.Devices
{
    public abstract class ConfigScreenControl : UserControl
    {
        public abstract void Apply();
    }
}
