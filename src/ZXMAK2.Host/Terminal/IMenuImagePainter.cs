using System;
using System.IO;

namespace ZXMAK2.Host.Terminal
{
    /// <summary>
    /// Optional PNG blitter for menu chrome (toolbar icons) on hosts that can decode images.
    /// </summary>
    public interface IMenuImagePainter
    {
        void DrawPng(int x, int y, int width, int height, string cacheKey, Stream png);
    }
}
