using System;
using System.Collections.Generic;
using System.Linq;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.Interfaces;

namespace Kozynax.UI
{
	public class JoystickSettings : SingleListViewDeviceSettings<IJoystickDevice, IHostDeviceInfo>
	{
		protected override IEnumerable<IHostDeviceInfo> GetListData()
			=> Host.Joystick.GetAvailableJoysticks();

		protected override IHostDeviceInfo FindSelectedItemInList(IJoystickDevice device)
			=> List.List.FirstOrDefault(d => d.HostId == device.HostId);

		protected override IJoystickDevice Apply(IHostDeviceInfo item)
		{
			var hdi = List.List[List.SelectedIndex];
			Device.HostId = hdi != null ? hdi.HostId : String.Empty;
			Init(BusManager, Host, Device);
			return Device;
		}
	}
}