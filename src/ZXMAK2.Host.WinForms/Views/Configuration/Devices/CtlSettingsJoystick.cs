using System;
using Kozui.Interfaces;
using Kozynax.UI;
using ZXMAK2.Engine;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Engine.Entities;


namespace ZXMAK2.Host.WinForms.Views.Configuration.Devices
{
    public partial class CtlSettingsJoystick : SingleListViewRenderer<IJoystickDevice, JoystickSettings, IHostDeviceInfo>, IComponentImplementation<JoystickSettings>
    {
        private JoystickSettings _joystickSettings = new JoystickSettings();
        
        public CtlSettingsJoystick()
        {
            InitializeComponent();
        }

        public void Init(JoystickSettings joystickSettings)
        {
            _joystickSettings = joystickSettings;
            Init(joystickSettings, cbxType);
        }
        
        public override void Init(BusManager bmgr, IHostService host, IJoystickDevice device)
            => _joystickSettings.Init(bmgr, host, device);

        public override void Apply()
            => _joystickSettings.Apply();

        private void cbxType_SelectedIndexChanged(object sender, EventArgs e)
            => _joystickSettings.List.SelectedIndex = cbxType.SelectedIndex;
    }
}
