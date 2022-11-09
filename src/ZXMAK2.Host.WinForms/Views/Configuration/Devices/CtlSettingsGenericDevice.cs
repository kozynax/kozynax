using System;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Hardware;


namespace ZXMAK2.Host.WinForms.Views.Configuration.Devices
{
    public partial class CtlSettingsGenericDevice : ConfigScreenControl<BusDeviceBase>
    {
        private BusManager m_bmgr;
        private BusDeviceBase m_device;

        public CtlSettingsGenericDevice()
        {
            InitializeComponent();
        }

        public override void Init(BusManager bmgr, IHostService host, BusDeviceBase device)
        {
            m_bmgr = bmgr;
            m_device = device;
            txtDevice.Text = device.Name;
            txtDescription.Text = device.Description?.Replace("\n", Environment.NewLine);
        }

        public string DeviceName { get { return m_device.Description; } }
        public string DeviceType { get { return m_device.Name; } }

        public override void Apply()
        {
        }
    }
}
