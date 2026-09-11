using System.Drawing;
using System.Windows.Forms;


namespace ZXMAK2.Host.WinForms.Views.Configuration.Devices
{
    public abstract class ConfigScreenControl : UserControl
    {
        public abstract void Apply();

        protected override void InitLayout()
        {
            Dock = DockStyle.Fill;
            base.InitLayout();
        }

        protected override void ScaleControl(SizeF factor, BoundsSpecified specified)
        {
            // Fill the host panel; do not let Font autoscaling grow this control paast the dialog
            if (Dock == DockStyle.Fill)
                specified &= ~BoundsSpecified.Size;
            base.ScaleControl(factor, specified);
        }
    }
}
