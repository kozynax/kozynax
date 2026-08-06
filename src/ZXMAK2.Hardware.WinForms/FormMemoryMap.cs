using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Kozynax.UI;
using ZXMAK2.Host.Presentation.Interfaces;
using ZXMAK2.Host.WinForms.BindingTools;
using ZXMAK2.Host.WinForms.Tools;
using ZXMAK2.Host.WinForms.Views;


namespace ZXMAK2.Hardware.WinForms
{
    public partial class FormMemoryMap : FormView, IMemoryMapView
    {
        private MemoryMap _memoryMap;
        private KozuiBinder _binder;

        public FormMemoryMap()
        {
            InitializeComponent();
        }

        public void Init(MemoryBase memory)
        {
            _binder?.Dispose();
            _memoryMap?.Close();

            _memoryMap = new MemoryMap(memory);
            _binder = new KozuiBinder();
            _binder.BindText(_memoryMap.Cmr0Value, lblCmrValue0);
            _binder.BindText(_memoryMap.Cmr1Value, lblCmrValue1);
            _binder.BindText(_memoryMap.Window0000, lblWnd0000);
            _binder.BindText(_memoryMap.Window4000, lblWnd4000);
            _binder.BindText(_memoryMap.Window8000, lblWnd8000);
            _binder.BindText(_memoryMap.WindowC000, lblWndC000);
            _binder.BindCheckBox(_memoryMap.Dosen, chkDosen, twoWay: false);
            _binder.BindCheckBox(_memoryMap.Sysen, chkSysen, twoWay: false);

            timerUpdate.Interval = _memoryMap.UpdateTimer.IntervalMs;
            timerUpdate.Enabled = _memoryMap.UpdateTimer.Enabled;

            propGrid.SelectedObject = new BusDeviceProxy(memory);
            var tc = TypeDescriptor.GetConverter(typeof(BusDeviceProxy));
            var propCount = tc
                .GetProperties(propGrid.SelectedObject)
                .Count;
            var gridVisible = propCount > 0;
            propGrid.Visible = gridVisible;
            if (!gridVisible)
            {
                ClientSize = new Size(
                    ClientSize.Width,
                    lblWndC000.Location.Y + lblWndC000.Height + 8);
            }
            else
            {
                var height = (propCount + 5) * 16;
                if (height > 1600)
                    height = 1600;
                propGrid.Height = height;
                ClientSize = new Size(
                    ClientSize.Width,
                    propGrid.Location.Y + propGrid.Height);
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            timerUpdate.Enabled = false;
            _binder?.Dispose();
            _binder = null;
            _memoryMap?.Close();
            _memoryMap = null;
            base.OnFormClosed(e);
        }

        private void timerUpdate_Tick(object sender, EventArgs e)
        {
            _memoryMap?.UpdateTimer.Tick();
            if (propGrid.Visible)
                propGrid.Refresh();
        }

        private void lblCmrValue0_DoubleClick(object sender, EventArgs e)
            => _memoryMap?.EditCmr0();

        private void lblCmrValue1_DoubleClick(object sender, EventArgs e)
            => _memoryMap?.EditCmr1();
    }
}
