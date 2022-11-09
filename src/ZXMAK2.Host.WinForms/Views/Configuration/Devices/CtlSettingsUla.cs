using System;
using Kozui.Interfaces;
using Kozynax.UI;
using ZXMAK2.Engine;
using ZXMAK2.Hardware;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Host.Entities;


namespace ZXMAK2.Host.WinForms.Views.Configuration.Devices
{
    public partial class CtlSettingsUla : ConfigScreenControl<UlaDeviceBase>, IComponentImplementation<UlaSettings>
    {
        private BusManager m_bmgr;
        private IHostService m_host;
        private UlaDeviceBase m_device;

        private UlaSettings _ula;
        
        public CtlSettingsUla()
        {
            InitializeComponent();

            cbxType.SelectedIndexChanged += CbxType_SelectedIndexChanged;
        }

        public void Init(UlaSettings ulaSettings)
        {
            _ula = ulaSettings;
            _ula.Redraw += Ula_Redraw;
            
            Ula_Redraw(this, EventArgs.Empty);
        }
        
        private void CbxType_SelectedIndexChanged(object sender, EventArgs e)
            => _ula.Devices.SelectedIndex = cbxType.SelectedIndex;

        public override void Init(BusManager bmgr, IHostService host, UlaDeviceBase device)
            => _ula.Init(bmgr, host, device);
        
        private void Ula_Redraw(object sender, EventArgs e)
        {
            cbxType.Items.Clear();
            foreach (var bdb in _ula.Devices.List)
                cbxType.Items.Add(bdb);
            cbxType.SelectedIndex = _ula.Devices.SelectedIndex;
        }

        public override void Apply()
            => _ula.Apply();
    }
}
