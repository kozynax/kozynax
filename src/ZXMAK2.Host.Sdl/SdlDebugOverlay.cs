using System;
using System.Drawing;
using System.Linq;
using Silk.NET.SDL;
using ZXMAK2.Engine;
using ZXMAK2.Host.Terminal;

namespace ZXMAK2.Host.SdlBackend
{
    /// <summary>
    /// On-screen debug stats (FPS / sizes / sound) for View → Debug Info.
    /// Mirrors the WinForms <c>OsdRenderer</c> text panel (without timing graphs).
    /// </summary>
    public sealed unsafe class SdlDebugOverlay
    {
        private const int GraphLength = 60;
        private const int FontScale = 2;
        private const int Pad = 6;

        private static readonly TerminalColor Bg = TerminalColor.Rgb(0, 128, 0);
        private static readonly TerminalColor Fg = TerminalColor.Rgb(255, 255, 0);

        private readonly GraphMonitor _graphRender = new GraphMonitor(GraphLength);
        private readonly GraphMonitor _graphLoad = new GraphMonitor(GraphLength);
        private readonly GraphMonitor _graphUpdate = new GraphMonitor(GraphLength);

        private int _frameStartTact;
        private int _sampleRate;
        private Size _frameSize;
        private bool _isRunning = true;

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (_isRunning == value)
                    return;
                _isRunning = value;
                _graphUpdate.ResetPeriod();
            }
        }

        public void OnFrame(SdlFrameDebugInfo info, Size pixelSize, float ratio)
        {
            _frameStartTact = info.StartTact;
            _sampleRate = info.SampleRate;
            _frameSize = new Size(
                pixelSize.Width,
                (int)(pixelSize.Height * ratio + 0.5f));

            if (!info.IsRefresh)
            {
                _graphUpdate.PushPeriod();
                _graphLoad.PushValue(info.UpdateTime);
            }
        }

        public void OnPresent()
            => _graphRender.PushPeriod();

        public void Draw(Sdl sdl, Renderer* renderer, int winW, int winH, int deviceFps, int uiScale = TerminalBase.DefaultUiScale)
        {
            if (sdl == null || renderer == null || winW <= 0 || winH <= 0)
                return;

            uiScale = Math.Max(TerminalBase.DefaultUiScale, uiScale);
            var fontScale = FontScale * uiScale;
            var pad = Pad * uiScale;

            var frequency = GraphMonitor.Frequency;
            var graphRender = _graphRender.Get();
            var graphUpdate = _graphUpdate.Get();
            var avgT = graphRender.Average() * 1000D / frequency;
            var avgU = graphUpdate.Average() * 1000D / frequency;
            var fpsRender = avgT > 0.0001 ? 1000D / avgT : 0D;
            var fpsUpdate = avgU > 0.0001 ? 1000D / avgU : 0D;

            var lines = new[]
            {
                string.Format("Render FPS: {0:F3}", fpsRender),
                string.Format("Update FPS: {0}", _isRunning ? fpsUpdate.ToString("F3") : "n/a"),
                string.Format("Device FPS: {0}", deviceFps > 0 ? deviceFps.ToString() : "?"),
                string.Format("Back: [{0}, {1}]", winW, winH),
                string.Format("Frame: [{0}, {1}]", _frameSize.Width, _frameSize.Height),
                string.Format("Sound: {0:F3} kHz", _sampleRate / 1000D),
                string.Format("FrameStart: {0}T", _frameStartTact),
            };

            var maxChars = lines.Max(l => l.Length);
            var lineH = TerminalFont.GlyphHeight * fontScale;
            var textW = maxChars * TerminalFont.GlyphWidth * fontScale;
            var textH = lines.Length * lineH;
            var boxW = textW + pad * 2;
            var boxH = textH + pad * 2;
            if (boxW > winW)
                boxW = winW;
            if (boxH > winH)
                boxH = winH;

            sdl.SetRenderDrawBlendMode(renderer, BlendMode.Blend);
            sdl.SetRenderDrawColor(renderer, Bg.R, Bg.G, Bg.B, 192);
            var bg = new Silk.NET.Maths.Rectangle<int>(0, 0, boxW, boxH);
            sdl.RenderFillRect(renderer, &bg);

            _drawSdl = sdl;
            _drawRenderer = renderer;
            var y = pad;
            foreach (var line in lines)
            {
                TerminalFont.Draw(FillGlyph, pad, y, line, fontScale, Fg);
                y += lineH;
            }
            _drawRenderer = null;
            _drawSdl = null;

            sdl.SetRenderDrawBlendMode(renderer, BlendMode.None);
        }

        private Sdl _drawSdl;
        private Renderer* _drawRenderer;

        private void FillGlyph(int x, int y, int w, int h, TerminalColor color)
        {
            if (_drawSdl == null || _drawRenderer == null)
                return;
            _drawSdl.SetRenderDrawColor(_drawRenderer, color.R, color.G, color.B, color.A);
            var rect = new Silk.NET.Maths.Rectangle<int>(x, y, w, h);
            _drawSdl.RenderFillRect(_drawRenderer, &rect);
        }
    }
}
