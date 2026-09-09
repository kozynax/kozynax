using System;
using System.Collections.Generic;
using System.IO;
using Silk.NET.SDL;
using StbImageSharp;

namespace ZXMAK2.Host.SdlBackend
{
    /// <summary>
    /// Caches PNG→SDL textures (StbImageSharp) for menu toolbar icons.
    /// </summary>
    public sealed unsafe class SdlMenuImagePainter : IDisposable, ZXMAK2.Host.Terminal.IMenuImagePainter
    {
        private readonly Sdl _sdl;
        private readonly Renderer* _renderer;
        private readonly Dictionary<string, IntPtr> _textures = new Dictionary<string, IntPtr>(StringComparer.Ordinal);
        private bool _disposed;

        public SdlMenuImagePainter(Sdl sdl, Renderer* renderer)
        {
            _sdl = sdl ?? throw new ArgumentNullException(nameof(sdl));
            _renderer = renderer;
        }

        public void DrawPng(int x, int y, int width, int height, string cacheKey, Stream png)
        {
            if (_disposed || _renderer == null || png == null || width <= 0 || height <= 0)
                return;

            var key = string.IsNullOrEmpty(cacheKey) ? Guid.NewGuid().ToString("N") : cacheKey;
            var texture = EnsureTexture(key, png);
            if (texture == null)
                return;

            var dst = new Silk.NET.Maths.Rectangle<int>(x, y, width, height);
            _sdl.SetTextureBlendMode(texture, BlendMode.Blend);
            _sdl.RenderCopy(_renderer, texture, null, &dst);
        }

        private Texture* EnsureTexture(string key, Stream png)
        {
            if (_textures.TryGetValue(key, out var existing) && existing != IntPtr.Zero)
                return (Texture*)existing;

            byte[] pngBytes;
            using (var ms = new MemoryStream())
            {
                if (png.CanSeek)
                    png.Position = 0;
                png.CopyTo(ms);
                pngBytes = ms.ToArray();
            }

            var image = ImageResult.FromMemory(pngBytes, ColorComponents.RedGreenBlueAlpha);
            if (image.Width <= 0 || image.Height <= 0 || image.Data == null)
                return null;

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
                _renderer,
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
            _textures[key] = (IntPtr)texture;
            return texture;
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
