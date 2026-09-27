using System;
using System.IO;
using ZXMAK2.Engine;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Presentation.Interfaces;

namespace ZXMAK2.Host.SdlBackend.Services
{
    public sealed class SdlSettingService : ISettingService
    {
        private const string FileName = "kozynax.ini";
        private const string WindowSection = "Window";
        private const string SdlSection = "SDL";

        public const int FontSizeNormal = -1;
        public const int FontSizeSmaller = -2;

        private readonly IniFile _ini;
        private int _windowWidth = 640;
        private int _windowHeight = 512;
        private int _fontSize = FontSizeNormal;

        public SdlSettingService()
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

        /// <summary>
        /// UI font vs window zoom: -1 default, -2 one step smaller.
        /// </summary>
        public int FontSize
        {
            get { return _fontSize; }
            set
            {
                var size = value <= FontSizeSmaller ? FontSizeSmaller : FontSizeNormal;
                if (_fontSize == size)
                    return;
                _fontSize = size;
                Save();
            }
        }

        public bool IsSmallerUiFont
            => _fontSize == FontSizeSmaller;

        public bool IsToolBarVisible { get; set; } = true;
        public bool IsStatusBarVisible { get; set; } = true;
        public SyncSource SyncSource { get; set; } = SyncSource.Sound;
        public ScaleMode RenderScaleMode { get; set; } = ScaleMode.KeepProportion;
        public VideoFilter RenderVideoFilter { get; set; } = VideoFilter.None;
        public bool RenderSmooth { get; set; }
        public bool RenderMimicTv { get; set; } = true;
        public bool RenderDisplayIcon { get; set; } = true;
        public bool RenderDebugInfo { get; set; }

        private void Load()
        {
            try
            {
                _ini.Load();
                var width = _ini.GetInt(WindowSection, "Width", _windowWidth);
                var height = _ini.GetInt(WindowSection, "Height", 0);
                if (height < 1)
                    height = _ini.GetInt(WindowSection, "Heigth", _windowHeight);
                if (width >= 1)
                    _windowWidth = width;
                if (height >= 1)
                    _windowHeight = height;
                var fontSize = _ini.GetInt(SdlSection, "FontSize", int.MinValue);
                if (fontSize == int.MinValue)
                    fontSize = _ini.GetInt("Render", "FontSize", FontSizeNormal);
                _fontSize = fontSize <= FontSizeSmaller ? FontSizeSmaller : FontSizeNormal;
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
                _ini.SetInt(SdlSection, "FontSize", _fontSize);
                _ini.RemoveSection("Render");
                _ini.Save();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
    }
}
