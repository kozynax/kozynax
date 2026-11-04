using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using RenderScaleMode = ZXMAK2.Host.Presentation.Interfaces.ScaleMode;

namespace ZXMAK2.Host.AvaloniaBackend
{
    /// <summary>
    /// Emulator framebuffer presenter: one <see cref="WriteableBitmap"/> re-filled per
    /// frame and drawn with the WinForms/SDL scale-mode rules.
    /// </summary>
    public sealed class FrameSurface : Control
    {
        private WriteableBitmap _bitmap;
        private int _frameWidth;
        private int _frameHeight;
        private float _frameRatio = 1f;
        private bool _smooth;
        private RenderScaleMode _scaleMode = RenderScaleMode.KeepProportion;

        /// <summary>Raised from the render pass after the latest frame was drawn.</summary>
        public event Action Presented;

        public FrameSurface()
        {
            ClipToBounds = true;
            Focusable = true;
            ApplyInterpolation();
        }

        public bool HasFrame => _bitmap != null;
        public int FrameWidth => _frameWidth;
        public int FrameHeight => _frameHeight;
        public float FrameRatio => _frameRatio;

        public RenderScaleMode ScaleMode
        {
            get => _scaleMode;
            set
            {
                if (_scaleMode == value)
                    return;
                _scaleMode = value;
                InvalidateVisual();
            }
        }

        public bool Smooth
        {
            get => _smooth;
            set
            {
                if (_smooth == value)
                    return;
                _smooth = value;
                ApplyInterpolation();
                InvalidateVisual();
            }
        }

        /// <summary>Copies a 32bpp ARGB frame into the bitmap. UI thread only.</summary>
        public void UpdateFrame(int[] buffer, int width, int height, float ratio)
        {
            if (buffer == null || width <= 0 || height <= 0)
                return;

            if (_bitmap == null || _frameWidth != width || _frameHeight != height)
            {
                _bitmap?.Dispose();
                _bitmap = new WriteableBitmap(
                    new PixelSize(width, height),
                    new Vector(96, 96),
                    PixelFormat.Bgra8888,
                    AlphaFormat.Opaque);
                _frameWidth = width;
                _frameHeight = height;
            }
            _frameRatio = ratio;

            using (var fb = _bitmap.Lock())
            {
                var rowInts = width;
                if (fb.RowBytes == width * 4)
                {
                    Marshal.Copy(buffer, 0, fb.Address, Math.Min(buffer.Length, width * height));
                }
                else
                {
                    for (var y = 0; y < height; y++)
                    {
                        var dst = fb.Address + y * fb.RowBytes;
                        Marshal.Copy(buffer, y * rowInts, dst, rowInts);
                    }
                }
            }

            InvalidateVisual();
        }

        public override void Render(DrawingContext context)
        {
            var bounds = Bounds;
            context.FillRectangle(Brushes.Black, new Rect(bounds.Size));

            if (_bitmap == null)
                return;

            var dst = ComputeDestination(
                _scaleMode,
                bounds.Width,
                bounds.Height,
                _frameWidth,
                _frameHeight,
                _frameRatio);
            context.DrawImage(_bitmap, new Rect(0, 0, _frameWidth, _frameHeight), dst);
            Presented?.Invoke();
        }

        private void ApplyInterpolation()
        {
            RenderOptions.SetBitmapInterpolationMode(
                this,
                _smooth ? BitmapInterpolationMode.HighQuality : BitmapInterpolationMode.None);
        }

        /// <summary>
        /// Mirrors WinForms <c>ScaleHelper.GetDestinationRect</c> / SDL <c>ComputeDestination</c>.
        /// </summary>
        public static Rect ComputeDestination(
            RenderScaleMode scaleMode,
            double winW,
            double winH,
            int frameW,
            int frameH,
            float ratio)
        {
            if (frameW <= 0 || frameH <= 0)
                return new Rect(0, 0, winW, winH);

            var dstW = (double)frameW;
            var dstH = frameH * Math.Max(ratio, 0.01f);
            var rx = winW / dstW;
            var ry = winH / dstH;

            switch (scaleMode)
            {
                case RenderScaleMode.SquarePixelSize:
                {
                    var s = Math.Min(Math.Floor(rx), Math.Floor(ry));
                    s = s < 1 ? 1 : s;
                    rx = ry = s;
                    break;
                }
                case RenderScaleMode.FixedPixelSize:
                    rx = Math.Floor(rx);
                    ry = Math.Floor(ry);
                    if (rx < 1)
                        rx = 1;
                    if (ry < 1)
                        ry = 1;
                    break;
                case RenderScaleMode.KeepProportion:
                    if (rx > ry)
                        rx = (winW * ry / rx) / dstW;
                    else if (rx < ry)
                        ry = (winH * rx / ry) / dstH;
                    break;
                case RenderScaleMode.Stretch:
                    break;
            }

            var outW = Math.Floor(dstW * rx);
            var outH = Math.Floor(dstH * ry);
            var x = Math.Floor((winW - outW) / 2);
            var y = Math.Floor((winH - outH) / 2);
            return new Rect(x, y, outW, outH);
        }

        public void DisposeBitmap()
        {
            _bitmap?.Dispose();
            _bitmap = null;
        }
    }
}
