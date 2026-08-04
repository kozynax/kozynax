using System;
using System.Drawing;
using System.IO;
using ZXMAK2.Host.Interfaces;

namespace ZXMAK2.Host.Entities
{
    public class IconDescriptor : IIconDescriptor
    {
        private readonly byte[] m_iconData;

        public string Name { get; private set; }
        public Size Size { get; private set; }
        public bool Visible { get; set; }

        public IconDescriptor(string iconName, Image iconImage)
        {
            Name = iconName;
            Size = iconImage.Size;
            using (var stream = new MemoryStream())
            {
                iconImage.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                m_iconData = stream.ToArray();
            }
        }

        public IconDescriptor(string iconName, Stream iconStream)
        {
            if (iconStream == null)
            {
                throw new FileNotFoundException(
                    string.Format("Icon stream '{0}' not found", iconName));
            }

            Name = iconName;
            using (var copy = new MemoryStream())
            {
                iconStream.CopyTo(copy);
                m_iconData = copy.ToArray();
            }

            Size = ReadImageSize(m_iconData);
        }

        public Stream GetImageStream()
        {
            return new MemoryStream(m_iconData);
        }

        private static Size ReadImageSize(byte[] data)
        {
            // Prefer PNG IHDR so Linux hosts do not need System.Drawing/GDI+.
            if (data.Length >= 24
                && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47
                && data[12] == (byte)'I' && data[13] == (byte)'H' && data[14] == (byte)'D' && data[15] == (byte)'R')
            {
                var width = (data[16] << 24) | (data[17] << 16) | (data[18] << 8) | data[19];
                var height = (data[20] << 24) | (data[21] << 16) | (data[22] << 8) | data[23];
                return new Size(width, height);
            }

            try
            {
                using (var stream = new MemoryStream(data))
                using (var bitmap = new Bitmap(stream))
                {
                    return bitmap.Size;
                }
            }
            catch (PlatformNotSupportedException)
            {
                return Size.Empty;
            }
            catch (TypeInitializationException)
            {
                return Size.Empty;
            }
        }
    }
}
