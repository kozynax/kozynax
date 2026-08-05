namespace ZXMAK2.Host.WinForms.Lib
{
    /// <summary>
    /// Side-list + detail layout (MachineSettings pattern) in cell units.
    /// Children use <see cref="KozuiControl.Dock"/>.
    /// </summary>
    public class DockPanel : Panel
    {
        private bool _lastChildFill = true;

        public bool LastChildFill
        {
            get => _lastChildFill;
            set => SetProperty(ref _lastChildFill, value);
        }
    }
}
