using System;
using System.Windows.Forms;
using Kozynax.UI;
using ZXMAK2.Engine;
using ZXMAK2.Host.Interfaces;

namespace ZXMAK2.Host.WinForms.Views.Configuration.Devices
{
	public class SingleListViewRenderer<TDevice, TSettings, TListItem> : ConfigScreenControl<TDevice>
		where TSettings : SingleListViewDeviceSettings<TDevice, TListItem>
	{
		private ComboBox _comboBox;
		private TSettings _deviceSettings;

		public void Init(TSettings deviceSettings, ComboBox comboBox)
		{
			_comboBox = comboBox;
			_comboBox.SelectedIndexChanged += CbxType_SelectedIndexChanged;

			_deviceSettings = deviceSettings;
			_deviceSettings.Redraw += Ula_Redraw;
            
			Ula_Redraw(this, EventArgs.Empty);
		}
        
		private void CbxType_SelectedIndexChanged(object sender, EventArgs e)
			=> _deviceSettings.List.SelectedIndex = _comboBox.SelectedIndex;

		public override void Init(BusManager bmgr, IHostService host, TDevice device)
			=> _deviceSettings.Init(bmgr, host, device);
        
		private void Ula_Redraw(object sender, EventArgs e)
		{
			_comboBox.Items.Clear();
			foreach (var bdb in _deviceSettings.List.List)
				_comboBox.Items.Add(bdb);
			_comboBox.SelectedIndex = _deviceSettings.List.SelectedIndex;
		}

		public override void Apply()
			=> _deviceSettings.Apply();
	}
}