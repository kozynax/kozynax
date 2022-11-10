using Kozui.Interfaces;
using Kozynax.UI;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Hardware.General;
using ZXMAK2.Host.Interfaces;


namespace ZXMAK2.Host.WinForms.Views.Configuration.Devices
{
    public partial class CtlSettingsHayesModem : SingleListViewRenderer<HayesModem, ModemSettings, string>, IComponentImplementation<ModemSettings>
    {
        public CtlSettingsHayesModem()
        {
            InitializeComponent();
        }

        public void Init(ModemSettings modemSettings)
            => base.Init(modemSettings, cmbPorts, label1);
    }
}
