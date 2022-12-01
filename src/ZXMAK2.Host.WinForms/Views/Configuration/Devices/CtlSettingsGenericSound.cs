using System;
using Kozui.Interfaces;
using Kozynax.UI;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Interfaces;

namespace ZXMAK2.Host.WinForms.Views.Configuration.Devices
{
    public partial class CtlSettingsGenericSound : ConfigScreenControl, IComponentImplementation<GenericSoundSettings, ISoundRenderer>
    {
        private GenericSoundSettings _settings;

        public CtlSettingsGenericSound()
        {
            InitializeComponent();
            trkVolume.ValueChanged += TrkVolume_ValueChanged;
        }

        public void Init(GenericSoundSettings settings)
        {
            _settings = settings;
            _settings.Redraw += (s, e) => {
                trkVolume.ValueChanged -= TrkVolume_ValueChanged;
                Redraw();
                trkVolume.ValueChanged += TrkVolume_ValueChanged;
            };
        }

        private void TrkVolume_ValueChanged(object sender, EventArgs e)
            => _settings.Volume.Value = trkVolume.Value;

        private void Redraw()
        {
            trkVolume.Minimum = _settings.Volume.Minimum;
            trkVolume.Maximum = _settings.Volume.Maximum;
            trkVolume.Value = _settings.Volume.Value;
        }
        
        public override void Apply()
            => _settings.Apply();
    }
}
