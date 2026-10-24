using System;
using System.IO;
using Kozynax.UI;
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

        private readonly IniFile _ini;
        private int _windowWidth = 640;
        private int _windowHeight = 512;
        private int _fontSize = UiFontSteps.Regular;
        private int _dbgFontSize = UiFontSteps.Regular;

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
        /// Menu font offset vs window zoom. See <see cref="UiFontSteps"/>.
        /// Saved as [SDL] FontSize.
        /// </summary>
        public int FontSize
        {
            get { return _fontSize; }
            set
            {
                var size = UiFontSteps.Clamp(value);
                if (_fontSize == size)
                    return;
                _fontSize = size;
                Save();
            }
        }

        /// <summary>
        /// Debugger font offset vs window zoom. Same steps as <see cref="FontSize"/>.
        /// Saved as [SDL] DbgFontSize.
        /// </summary>
        public int DbgFontSize
        {
            get { return _dbgFontSize; }
            set
            {
                var size = UiFontSteps.Clamp(value);
                if (_dbgFontSize == size)
                    return;
                _dbgFontSize = size;
                Save();
            }
        }

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
                    fontSize = _ini.GetInt("Render", "FontSize", UiFontSteps.Regular);
                _fontSize = UiFontSteps.Clamp(fontSize);
                _dbgFontSize = UiFontSteps.Clamp(
                    _ini.GetInt(SdlSection, "DbgFontSize", UiFontSteps.Regular));
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
                _ini.SetInt(SdlSection, "DbgFontSize", _dbgFontSize);
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
