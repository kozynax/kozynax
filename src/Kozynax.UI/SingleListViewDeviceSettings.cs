using System.Collections.Generic;
using ZXMAK2.Engine;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.WinForms.Lib;

namespace Kozynax.UI
{
    public abstract class SingleListViewDeviceSettings<TDevice, TListItem> : DeviceSettings<TDevice>
    {
        protected BusManager BusManager { get; private set; }
        protected IHostService Host { get; private set; }
        public TDevice Device { get; private set; }

        public ListView<TListItem> List { get; }
        public Label Title { get; }

        protected abstract IEnumerable<TListItem> GetListData();
        protected abstract TListItem FindSelectedItemInList(TDevice device);
        protected abstract TDevice Apply(TListItem item);

        public SingleListViewDeviceSettings()
        {
            List = new ListView<TListItem>();
            Title = new Label();
            Title.Text = ListLabel;
        }

        protected abstract string ListLabel { get; }

        public override void Init(BusManager bmgr, IHostService host, TDevice device)
        {
            BusManager = bmgr;
            Host = host;
            Device = device;

            List.Reset(GetListData());

            List.SelectedIndex = -1;
            if (device != null)
            {
                var ourItem = FindSelectedItemInList(device);
                List.SelectedIndex = List.List.IndexOf(ourItem);
            }
        }

        public override void Apply()
        {
            if (List.SelectedIndex < 0)
                return;

            var bdd = List.List[List.SelectedIndex];
            var updatedDevice = Apply(bdd);
            Init(BusManager, Host, updatedDevice);
        }
    }
}
