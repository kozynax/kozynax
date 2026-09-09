using System;
using System.Collections.Generic;
using System.Linq;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Hardware.General;
using ZXMAK2.Host.Interfaces;

namespace Kozynax.UI
{
	public class ModemSettings : SingleListViewDeviceSettings<HayesModem, string>
	{
		protected override string ListLabel => "Select COM-port";
		protected override IEnumerable<string> GetListData()
		{
			var list = new List<string> { "NONE" };
			list.AddRange(System.IO.Ports.SerialPort.GetPortNames());
			return list;
		}

		protected override string FindSelectedItemInList(HayesModem device)
			=> List.List.FirstOrDefault(d => d == device.PortName);

		protected override HayesModem Apply(string item)
		{
			if (List.SelectedIndex == 0)
				Device.PortName = string.Empty;
			else
				Device.PortName = List.List[List.SelectedIndex];

			return Device;
		}
	}
}