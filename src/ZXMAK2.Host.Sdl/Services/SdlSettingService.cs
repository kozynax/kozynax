using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Presentation.Interfaces;

namespace ZXMAK2.Host.SdlBackend.Services
{
    public sealed class SdlSettingService : ISettingService
    {
        public int WindowWidth { get; set; } = 640;
        public int WindowHeight { get; set; } = 512;
        public bool IsToolBarVisible { get; set; } = true;
        public bool IsStatusBarVisible { get; set; } = true;
        public SyncSource SyncSource { get; set; } = SyncSource.Sound;
        public ScaleMode RenderScaleMode { get; set; } = ScaleMode.KeepProportion;
        public VideoFilter RenderVideoFilter { get; set; } = VideoFilter.None;
        public bool RenderSmooth { get; set; }
        public bool RenderMimicTv { get; set; } = true;
        public bool RenderDisplayIcon { get; set; } = true;
        public bool RenderDebugInfo { get; set; }
    }
}
