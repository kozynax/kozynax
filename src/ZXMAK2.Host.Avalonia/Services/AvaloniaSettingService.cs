using System;
using System.IO;
using ZXMAK2.Engine;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Presentation.Interfaces;
using ZXMAK2.Host.SdlBackend.Services;

namespace ZXMAK2.Host.AvaloniaBackend.Services
{
    /// <summary>
    /// Shares <c>kozynax.ini</c> [Window] with the SDL host; Avalonia-specific keys go to [Avalonia].
    /// </summary>
    public sealed class AvaloniaSettingService : ISettingService
    {
        private const string FileName = "kozynax.ini";
        private const string WindowSection = "Window";
        private const string HostSection = "Avalonia";

        private readonly IniFile _ini;
        private int _windowWidth = 640;
        private int _windowHeight = 512;
        private ScaleMode _scaleMode = ScaleMode.KeepProportion;
        private bool _smooth;

        public AvaloniaSettingService()
        {
            _ini = new IniFile(Path.Combine(Utils.GetAppDataFolder(), FileName));
            Load();
        }

        public int WindowWidth
        {
            get { return _windowWidth; }
            set
            {
                var w = Math.Max(1, value);
                if (_windowWidth == w)
                    return;
                _windowWidth = w;
                Save();
            }
        }

        public int WindowHeight
        {
            get { return _windowHeight; }
            set
            {
                var h = Math.Max(1, value);
                if (_windowHeight == h)
                    return;
                _windowHeight = h;
                Save();
            }
        }

        public bool IsToolBarVisible { get; set; } = true;
        public bool IsStatusBarVisible { get; set; } = true;
        public SyncSource SyncSource { get; set; } = SyncSource.Sound;

        public ScaleMode RenderScaleMode
        {
            get { return _scaleMode; }
            set
            {
                if (_scaleMode == value)
                    return;
                _scaleMode = value;
                Save();
            }
        }

        public VideoFilter RenderVideoFilter { get; set; } = VideoFilter.None;

        public bool RenderSmooth
        {
            get { return _smooth; }
            set
            {
                if (_smooth == value)
                    return;
                _smooth = value;
                Save();
            }
        }

        public bool RenderMimicTv { get; set; } = true;
        public bool RenderDisplayIcon { get; set; } = true;
        public bool RenderDebugInfo { get; set; }

        private void Load()
        {
            try
            {
                _ini.Load();
                var width = _ini.GetInt(WindowSection, "Width", _windowWidth);
                var height = _ini.GetInt(WindowSection, "Height", _windowHeight);
                if (width >= 1)
                    _windowWidth = width;
                if (height >= 1)
                    _windowHeight = height;

                var scale = _ini.GetInt(HostSection, "ScaleMode", (int)_scaleMode);
                if (Enum.IsDefined(typeof(ScaleMode), scale))
                    _scaleMode = (ScaleMode)scale;
                _smooth = _ini.GetInt(HostSection, "Smooth", _smooth ? 1 : 0) != 0;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void Save()
        {
            try
            {
                _ini.SetInt(WindowSection, "Width", _windowWidth);
                _ini.SetInt(WindowSection, "Height", _windowHeight);
                _ini.SetInt(HostSection, "ScaleMode", (int)_scaleMode);
                _ini.SetInt(HostSection, "Smooth", _smooth ? 1 : 0);
                _ini.Save();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}
