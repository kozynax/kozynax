using System;
using System.IO;
using ZXMAK2.Engine.Interfaces;


namespace ZXMAK2.Serializers.ScreenshotSerializers
{
    public abstract class ScreenshotSerializerBase : FormatSerializer
    {
        protected IUlaDevice m_ulaDevice;

        public ScreenshotSerializerBase(IUlaDevice ulaDevice)
		{
            m_ulaDevice = ulaDevice;
		}

        #region FormatSerializer

        public override string FormatGroup { get { return "Screenshots"; } }
        public override string FormatName { get { return string.Format("{0} screenshot", FormatExtension); } }

        public override bool CanSerialize { get { return true; } }

        public override void Serialize(Stream stream)
        {
            var videoData = m_ulaDevice.VideoData;
            var size = videoData.Size;
            var scaledHeight = Math.Max(1, (int)(size.Height * videoData.Ratio));
            var rgba = ScaleToRgba(videoData.Buffer, size.Width, size.Height, scaledHeight);
            Save(stream, rgba, size.Width, scaledHeight);
        }

        #endregion

        protected abstract void Save(Stream stream, byte[] rgba, int width, int height);

        /// <summary>
        /// Convert ARGB ints to RGBA bytes and nearest-neighbor scale height by <paramref name="scaledHeight"/>.
        /// </summary>
        private static byte[] ScaleToRgba(int[] argb, int width, int height, int scaledHeight)
        {
            var rgba = new byte[width * scaledHeight * 4];
            for (var y = 0; y < scaledHeight; y++)
            {
                var srcY = height == scaledHeight
                    ? y
                    : (int)((long)y * height / scaledHeight);
                var srcRow = srcY * width;
                var dstRow = y * width * 4;
                for (var x = 0; x < width; x++)
                {
                    var pixel = argb[srcRow + x];
                    var i = dstRow + x * 4;
                    rgba[i] = (byte)(pixel >> 16);       // R
                    rgba[i + 1] = (byte)(pixel >> 8);    // G
                    rgba[i + 2] = (byte)pixel;           // B
                    rgba[i + 3] = (byte)(pixel >> 24);   // A
                }
            }
            return rgba;
        }
    }
}
