using System;
using System.IO;

namespace ZXMAK2.Host.WinForms.Lib
{
    /// <summary>
    /// Bitmap placeholder for hosts that can blit PNGs (e.g. SDL via <c>IMenuImagePainter</c>).
    /// </summary>
    public class ImageView : KozuiControl
    {
        private Func<Stream> _image;
        private string _imageKey;
        private int _sourceWidth;
        private int _sourceHeight;

        /// <summary>Opens a PNG stream for each draw (caller may return a fresh stream).</summary>
        public Func<Stream> Image
        {
            get => _image;
            set => SetProperty(ref _image, value);
        }

        /// <summary>Stable cache key for texture atlases.</summary>
        public string ImageKey
        {
            get => _imageKey;
            set => SetProperty(ref _imageKey, value);
        }

        /// <summary>Natural pixel width (used for aspect-fit when drawing).</summary>
        public int SourceWidth
        {
            get => _sourceWidth;
            set => SetProperty(ref _sourceWidth, Math.Max(0, value));
        }

        /// <summary>Natural pixel height (used for aspect-fit when drawing).</summary>
        public int SourceHeight
        {
            get => _sourceHeight;
            set => SetProperty(ref _sourceHeight, Math.Max(0, value));
        }
    }
}
