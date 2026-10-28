using System.IO;
using StbImageWriteSharp;
using ZXMAK2.Engine.Interfaces;


namespace ZXMAK2.Serializers.ScreenshotSerializers
{
    public class JpgSerializer : ScreenshotSerializerBase
    {
		public JpgSerializer(IUlaDevice ulaDevice)
            : base (ulaDevice)
		{
		}

        public override string FormatExtension { get { return "JPG"; } }

        protected override void Save(Stream stream, byte[] rgba, int width, int height)
        {
            new ImageWriter().WriteJpg(rgba, width, height, ColorComponents.RedGreenBlueAlpha, stream, 95);
        }
    }
}
