using System;
using System.Collections.Generic;
using Kozui.Abstract;
using ZXMAK2;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Host.WinForms.Lib.Layout;

namespace Kozynax.UI
{
	public class AddDeviceDialog : ViewDescription<AddDeviceDialog>
	{
		public event EventHandler CloseRequested;

		public Panel Root { get; }
		public ListView<BusDeviceCategory> Categories { get; }
		public ListView<BusDeviceDescriptor> Devices { get; }
		public TextView DeviceDescription { get; }
		public Button Finish { get; }
		public Button Cancel { get; }

		public List<BusDeviceBase> IgnoreList { get; }
		public BusDeviceBase Result { get; set; }
		public DlgResult DialogResult { get; private set; } = DlgResult.Cancel;

		public AddDeviceDialog(List<BusDeviceBase> ignoreList)
		{
			IgnoreList = ignoreList;

			Categories = new ListView<BusDeviceCategory>
			{
				ItemTextSelector = c => c.ToString(),
				Dock = Dock.Left,
				MinWidth = 14,
			};
			Categories.SelectedIndexChanged += Categories_SelectedIndexChanged;

			Devices = new ListView<BusDeviceDescriptor>
			{
				ItemTextSelector = d => d?.Name ?? string.Empty,
				Dock = Dock.Fill,
			};
			Devices.SelectedIndexChanged += Devices_SelectedIndexChanged;

			DeviceDescription = new TextView();

			Finish = new Button { Text = "Add", Enabled = false };
			Finish.Clicked += Finish_Clicked;

			Cancel = new Button { Text = "Cancel" };
			Cancel.Clicked += (_, __) => Complete(DlgResult.Cancel);

			Root = BuildTree();
			BindCategories();
			if (Categories.Count > 0)
				Categories.SelectedIndex = 0;
		}

		private Panel BuildTree()
		{
			var title = new Label
			{
				Text = "Add Device",
				Dock = Dock.Top,
				Margin = new Thickness(0, 0, 0, 1),
			};

			var desc = new Label
			{
				Text = string.Empty,
				Dock = Dock.Bottom,
				Margin = new Thickness(0, 1, 0, 0),
				MinHeight = 3,
			};
			// Mirror TextView into Label for Terminal (TextView not drawn specially as multi-line yet)
			DeviceDescription.PropertyChanged += (_, e) =>
			{
				if (e.PropertyName == null || e.PropertyName == nameof(TextView.Text))
					desc.Text = DeviceDescription.Text ?? string.Empty;
			};

			var buttons = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				Spacing = 2,
				Dock = Dock.Bottom,
				Margin = new Thickness(0, 1, 0, 0),
				HorizontalAlignment = HorizontalAlignment.Right,
			};
			buttons.Add(Finish);
			buttons.Add(Cancel);

			var lists = new DockPanel { Dock = Dock.Fill };
			lists.Add(Categories);
			lists.Add(Devices);

			var root = new DockPanel { Margin = new Thickness(1) };
			root.Add(title);
			root.Add(desc);
			root.Add(buttons);
			root.Add(lists);
			return root;
		}

		private void Finish_Clicked(object sender, EventArgs e)
		{
			var deviceIndex = Devices.SelectedIndex;
			if (deviceIndex < 0 || deviceIndex >= Devices.List.Count)
				return;

			var device = Devices.List[deviceIndex];
			if (device == null)
				return;

			try
			{
				Result = (BusDeviceBase)Activator.CreateInstance(device.Type);
				Complete(DlgResult.OK);
			}
			catch (Exception exception)
			{
				Logger.Error(exception);
			}
		}

		private void Complete(DlgResult result)
		{
			DialogResult = result;
			CloseRequested?.Invoke(this, EventArgs.Empty);
		}

		public void RequestCancel() => Complete(DlgResult.Cancel);

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
			if (index < 0 || index >= Categories.List.Count)
				return;
			var category = Categories.List[index];
			BindDevices(category);
		}

		public void BindCategories()
		{
			var list = new List<BusDeviceCategory>();
			foreach (var bdd in DeviceEnumerator.SelectWithout(GetIgnoreTypes()))
			{
				if (!list.Contains(bdd.Category))
					list.Add(bdd.Category);
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
			Devices.SelectedIndex = Devices.List.Count > 0 ? 0 : -1;
		}

		private IEnumerable<Type> GetIgnoreTypes()
		{
			var ignoreTypes = new List<Type>();
			foreach (var bdd in IgnoreList)
				ignoreTypes.Add(bdd.GetType());
			return ignoreTypes;
		}

		private static int DeviceNameComparison(
			BusDeviceDescriptor left,
			BusDeviceDescriptor right)
		{
			if (left == null && right == null)
				return 0;
			if (left != null && right != null)
				return left.Name.CompareTo(right.Name);
			return left == null ? -1 : 1;
		}
	}
}
