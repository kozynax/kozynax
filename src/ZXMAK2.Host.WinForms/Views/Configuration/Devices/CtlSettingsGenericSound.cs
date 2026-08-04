using Kozui.Interfaces;
using Kozynax.UI;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.WinForms.BindingTools;

namespace ZXMAK2.Host.WinForms.Views.Configuration.Devices
{
    public partial class CtlSettingsGenericSound : ConfigScreenControl, IComponentImplementation<GenericSoundSettings, ISoundRenderer>
    {
        private GenericSoundSettings _settings;
        private KozuiBinder _binder;

        public CtlSettingsGenericSound()
        {
            InitializeComponent();
        }

        public void Init(GenericSoundSettings settings)
        {
            _settings = settings;
            _binder?.Dispose();
            _binder = new KozuiBinder();
            _binder.BindTrackBar(_settings.Volume, trkVolume);
        }

        public override void Apply()
            => _settings.Apply();

        internal void DisposeBinder()
            => _binder?.Dispose();
    }
}
