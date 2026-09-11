using System.IO;
using Silk.NET.SDL;
using StbImageSharp;
using ZXMAK2.Resources;

namespace ZXMAK2.Host.SdlBackend
{
    /// <summary>
    /// Applies the embedded app PNG as the SDL window / taskbar icon.
    /// </summary>
    public static unsafe class SdlWindowIcon
    {
        public static void Apply(Sdl sdl, Window* window)
        {
            if (sdl == null || window == null)
                return;

            try
            {
                byte[] pngBytes;
                using (var stream = ResourceImages.IconAppPng)
                using (var ms = new MemoryStream())
                {
                    stream.CopyTo(ms);
                    pngBytes = ms.ToArray();
                }

                var image = ImageResult.FromMemory(pngBytes, ColorComponents.RedGreenBlueAlpha);
                if (image.Width <= 0 || image.Height <= 0 || image.Data == null)
                    return;

                var pixels = image.Data;
                fixed (byte* p = pixels)
                {
                    var surface = sdl.CreateRGBSurfaceFrom(
                        p,
                        image.Width,
                        image.Height,
                        32,
                        image.Width * 4,
                        0x000000FFu,
                        0x0000FF00u,
                        0x00FF0000u,
                        0xFF000000u);
                    if (surface == null)
                        return;
                    sdl.SetWindowIcon(window, surface);
                    sdl.FreeSurface(surface);
                }
            }
            catch
            {
                // Title-bar icon is optional; never block startup.
            }
        }
    }
}
