using System;
using System.Collections.Generic;
using System.IO;
using Silk.NET.SDL;
using StbImageSharp;
using ZXMAK2.Host.Interfaces;

namespace ZXMAK2.Host.SdlBackend
{
    /// <summary>
    /// Draws frame OSD icons (pause, disk, tape, …) in the top-right corner,
    /// matching WinForms <c>IconRenderer</c>.
    /// </summary>
    public sealed unsafe class SdlIconOverlay : IDisposable
    {
        private const int DisplaySize = 32;

        private readonly Sdl _sdl;
        private readonly Dictionary<string, IntPtr> _textures = new Dictionary<string, IntPtr>(StringComparer.Ordinal);
        private bool _disposed;

        public SdlIconOverlay(Sdl sdl)
        {
            _sdl = sdl ?? throw new ArgumentNullException(nameof(sdl));
        }

        public void Draw(Renderer* renderer, int winW, int winH, IIconDescriptor[] icons)
        {
            if (_disposed || renderer == null || icons == null || icons.Length == 0 || winW <= 0 || winH <= 0)
                return;

            var iconNumber = 1;
            foreach (var icon in icons)
            {
                if (icon == null || !icon.Visible)
                    continue;

                var texture = EnsureTexture(renderer, icon);
                if (texture == null)
                    continue;

                var dst = new Silk.NET.Maths.Rectangle<int>(
                    winW - DisplaySize * iconNumber,
                    0,
                    DisplaySize,
                    DisplaySize);
                _sdl.SetTextureBlendMode(texture, BlendMode.Blend);
                _sdl.RenderCopy(renderer, texture, null, &dst);
                iconNumber++;
            }
        }

        private Texture* EnsureTexture(Renderer* renderer, IIconDescriptor icon)
        {
            var key = icon.Name ?? string.Empty;
            if (_textures.TryGetValue(key, out var existing) && existing != IntPtr.Zero)
                return (Texture*)existing;

            var texture = LoadTexture(renderer, icon);
            if (texture != null)
                _textures[key] = (IntPtr)texture;
            return texture;
        }

        private Texture* LoadTexture(Renderer* renderer, IIconDescriptor icon)
        {
            try
            {
                byte[] pngBytes;
                using (var stream = icon.GetImageStream())
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    pngBytes = ms.ToArray();
                }

                // System.Drawing is Windows-only; StbImageSharp works on Linux.
                var image = ImageResult.FromMemory(pngBytes, ColorComponents.RedGreenBlueAlpha);
                if (image.Width <= 0 || image.Height <= 0 || image.Data == null)
                    return null;

                // Convert RGBA → BGRA for SDL_PIXELFORMAT_ARGB8888 on little-endian.
                var rgba = image.Data;
                var bgra = new byte[rgba.Length];
                for (var i = 0; i < rgba.Length; i += 4)
                {
                    bgra[i] = rgba[i + 2];
                    bgra[i + 1] = rgba[i + 1];
                    bgra[i + 2] = rgba[i];
                    bgra[i + 3] = rgba[i + 3];
                }

                var texture = _sdl.CreateTexture(
                    renderer,
                    Sdl.PixelformatArgb8888,
                    (int)TextureAccess.Static,
                    image.Width,
                    image.Height);
                if (texture == null)
                    return null;

                fixed (byte* p = bgra)
                {
                    _sdl.UpdateTexture(texture, null, p, image.Width * 4);
                }

                _sdl.SetTextureBlendMode(texture, BlendMode.Blend);
                return texture;
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            foreach (var ptr in _textures.Values)
            {
                if (ptr != IntPtr.Zero)
                    _sdl.DestroyTexture((Texture*)ptr);
            }
            _textures.Clear();
        }
    }
}
