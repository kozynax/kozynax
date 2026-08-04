using System.Windows.Forms;
using Kozynax.UI;
using ZXMAK2.Host.WinForms.BindingTools;

namespace ZXMAK2.Host.WinForms.Views.Configuration.Devices
{
	public class SingleListViewRenderer<TDevice, TSettings, TListItem> : ConfigScreenControl
		where TSettings : SingleListViewDeviceSettings<TDevice, TListItem>
	{
		private TSettings _deviceSettings;
		private KozuiBinder _binder;

		public void Init(TSettings deviceSettings, ComboBox comboBox, Label title)
		{
			_deviceSettings = deviceSettings;
			_binder?.Dispose();
			_binder = new KozuiBinder();
			_binder.BindComboBoxList(deviceSettings.List, comboBox);
			_binder.BindText(deviceSettings.Title, title);
		}

		public override void Apply()
			=> _deviceSettings.Apply();

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				_binder?.Dispose();
			base.Dispose(disposing);
		}
	}
}
