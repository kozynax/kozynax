using System.IO;
using StbImageWriteSharp;
using ZXMAK2.Engine.Interfaces;


namespace ZXMAK2.Serializers.ScreenshotSerializers
{
    public class PngSerializer : ScreenshotSerializerBase
    {
        public PngSerializer(IUlaDevice ulaDevice)
            : base(ulaDevice)
        {
        }

        public override string FormatExtension { get { return "PNG"; } }

        protected override void Save(Stream stream, byte[] rgba, int width, int height)
        {
            new ImageWriter().WritePng(rgba, width, height, ColorComponents.RedGreenBlueAlpha, stream);
        }
    }
}
