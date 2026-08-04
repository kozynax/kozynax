using System;
using System.Collections.Generic;
using Kozui.Abstract;
using Kozui.Interfaces;
using ZXMAK2;
using ZXMAK2.Dependency;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Presentation.Interfaces;
using ZXMAK2.Host.WinForms.Lib;

namespace Kozynax.UI
{
	public class AddDeviceDialog : ViewDescription<AddDeviceDialog>
	{
		public event EventHandler CloseRequested;
		
		public ListView<BusDeviceCategory> Categories { get; }
		public ListView<BusDeviceDescriptor> Devices { get; }
		public TextView DeviceDescription { get; }
		public Button Finish { get; }
		
		public List<BusDeviceBase> IgnoreList { get; }
		public BusDeviceBase Result { get; set; }
		public AddDeviceDialog(List<BusDeviceBase> ignoreList)
		{
			IgnoreList = ignoreList;
			
			Categories = new ListView<BusDeviceCategory>();
			Categories.SelectedIndexChanged += Categories_SelectedIndexChanged;

			Devices = new ListView<BusDeviceDescriptor>();
			Devices.SelectedIndexChanged += Devices_SelectedIndexChanged;

			DeviceDescription = new TextView();
			
			Finish = new Button();
			Finish.Clicked += Finish_Clicked;
		}

		private void Finish_Clicked(object sender, EventArgs e)
		{
			var deviceIndex = Devices.SelectedIndex;
			if (deviceIndex >= 0)
			{
				var device = Devices.List[deviceIndex];
				if (device != null)
				{
					try
					{
						Result = (BusDeviceBase)Activator.CreateInstance(device.Type);
						CloseRequested?.Invoke(this, EventArgs.Empty);
					}
					catch (Exception exception)
					{
						Logger.Error(exception);
					}
				}
			}
		}

		private void Devices_SelectedIndexChanged(object sender, int index)
		{
			if (index < 0 || index >= Devices.List.Count)
			{
				DeviceDescription.Text = string.Empty;
				Finish.Enabled = false;
				return;
			}

			var bdd = Devices.List[index]; 
			DeviceDescription.Text = bdd.Description;
			Finish.Enabled = true;
		}

		private void Categories_SelectedIndexChanged(object sender, int index)
		{
			var category = Categories.List[index];
			BindDevices(category);
		}

		public void BindCategories()
		{
			var list = new List<BusDeviceCategory>();
			foreach (var bdd in DeviceEnumerator.SelectWithout(GetIgnoreTypes()))
			{
				if (!list.Contains(bdd.Category))
				{
					list.Add(bdd.Category);
				}
			}
			list.Sort();
			Categories.Reset(list);
		}
		
		public void BindDevices(BusDeviceCategory category)
		{
			var list = new List<BusDeviceDescriptor>();
			list.AddRange(DeviceEnumerator.SelectByCategoryWithout(category, GetIgnoreTypes()));
			list.Sort(DeviceNameComparison);
			Devices.Reset(list);
		}
		
		private IEnumerable<Type> GetIgnoreTypes()
		{
			var ignoreTypes = new List<Type>();
			foreach (var bdd in IgnoreList)
			{
				ignoreTypes.Add(bdd.GetType());
			}
			return ignoreTypes;
		}
		
		private static int DeviceNameComparison(
			BusDeviceDescriptor left,
			BusDeviceDescriptor right)
		{
			if (left == null && right == null)
			{
				return 0;
			}
			if (left != null && right != null)
			{
				return left.Name.CompareTo(right.Name);
			}
			return left == null ? -1 : 1;
		}
	}
}