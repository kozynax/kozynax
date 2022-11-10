using System;
using System.Collections.Generic;
using System.Linq;
using ZXMAK2.Engine;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.WinForms.Lib;

namespace Kozynax.UI
{
    public abstract class SingleListViewDeviceSettings<TDevice, TListItem> : DeviceSettings<TDevice>
    {
        public event EventHandler Redraw;

        protected BusManager BusManager { get; private set; }
        private IHostService _host;
        public ListView<TListItem> List { get; }
        protected abstract List<TListItem> GetListData();
        protected abstract TListItem FindSelectedItemInList(TDevice device);
        protected abstract TDevice Apply(TListItem item);
        
        public SingleListViewDeviceSettings()
        {
            List = new ListView<TListItem>();
        }

        public override void Init(BusManager bmgr, IHostService host, TDevice device)
        {
            BusManager = bmgr;
            _host = host;

            var list = GetListData();
            List.List.Clear();
            foreach (var d in list)
                List.List.Add(d);
            
            List.SelectedIndex = -1;
            if (device != null)
            {
                var ourItem = FindSelectedItemInList(device);
                List.SelectedIndex = List.List.IndexOf(ourItem);
            }
            
            Redraw?.Invoke(this, EventArgs.Empty);
        }

        public override void Apply()
        {
            if (List.SelectedIndex < 0)
                return;

            var bdd = List.List[List.SelectedIndex];
            var updatedDevice = Apply(bdd);
            Init(BusManager, _host, updatedDevice);
        }
    }
}