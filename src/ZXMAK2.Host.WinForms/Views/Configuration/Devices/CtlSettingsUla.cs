using Kozui.Interfaces;
using Kozynax.UI;
using ZXMAK2.Engine;
using ZXMAK2.Hardware;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Engine.Entities;

namespace ZXMAK2.Host.WinForms.Views.Configuration.Devices
{
    public partial class CtlSettingsUla : SingleListViewRenderer<UlaDeviceBase, UlaSettings, BusDeviceDescriptor>, IComponentImplementation<UlaSettings>
    {
        private BusManager m_bmgr;
        private IHostService m_host;
        private UlaDeviceBase m_device;

        private UlaSettings _ula;
        
        public CtlSettingsUla()
        {
            InitializeComponent();
        }

        public void Init(UlaSettings ulaSettings)
            => Init(ulaSettings, cbxType, lblType);
    }
}
