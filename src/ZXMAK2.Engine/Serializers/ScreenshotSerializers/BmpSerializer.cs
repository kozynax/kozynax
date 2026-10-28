using System.IO;
using StbImageWriteSharp;
using ZXMAK2.Engine.Interfaces;


namespace ZXMAK2.Serializers.ScreenshotSerializers
{
    public class BmpSerializer : ScreenshotSerializerBase
    {
        public BmpSerializer(IUlaDevice ulaDevice)
            : base(ulaDevice)
        {
        }

        public override string FormatExtension { get { return "BMP"; } }

        protected override void Save(Stream stream, byte[] rgba, int width, int height)
        {
            new ImageWriter().WriteBmp(rgba, width, height, ColorComponents.RedGreenBlueAlpha, stream);
        }
    }
}
