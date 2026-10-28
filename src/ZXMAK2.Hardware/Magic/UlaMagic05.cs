namespace ZXMAK2.Hardware.Magic
{
    public class UlaMagic05 : UlaDeviceBase
    {
        private const int RcIntLength = 34;            // Most likely target was 32, but with my actual RC chain it is closer to 34
        private RcInt _rcInt = new RcInt(RcIntLength); // never seen above 35 in minfo, it changes 34..35 and sometimes 0
        
        public UlaMagic05()
        {
            Name = "MAGIC-05";
        }

        protected override SpectrumRendererParams CreateSpectrumRendererParams()
        {
            // Magic-05 (and Quorum 64)
            // Total Size:          448 x 312
            // Visible Size:        384 x 296 (72+256+56 x 64+192+40)
            var timing = SpectrumRenderer.CreateParams();
            timing.c_ulaLineTime = 224;

            timing.c_ulaFirstPaperLine = 64;
            timing.c_ulaFirstPaperTact = 44;
            timing.c_frameTactCount = 69888;

            timing.c_ulaBorderTop = 32;
            timing.c_ulaBorderBottom = 32;
            timing.c_ulaBorderLeftT = 16;
            timing.c_ulaBorderRightT = 16;

            timing.c_ulaIntBegin = 0;
            
            // Nominal only, we'll have dynamic INT length for every frame
            timing.c_ulaIntLength = RcIntLength;

            timing.c_ulaWidth = (timing.c_ulaBorderLeftT + 128 + timing.c_ulaBorderRightT) * 2;
            timing.c_ulaHeight = (timing.c_ulaBorderTop + 192 + timing.c_ulaBorderBottom);
            return timing;
        }

        protected override void BeginFrame()
        {
            _rcInt.OnBeginFrame();
            base.BeginFrame();
        }

        public override bool CheckInt(int frameTact)
        {
            return frameTact < _rcInt.IntLength;
        }
        
        protected override void WritePortFE(ushort addr, byte value, ref bool handled)
        {
            // D2: A2=A3=A4=1, D3: active ~POUT, so A[1..0] = '10', so A[4..0] = '11110'
            if ((addr & 0x1F) == (0xFE & 0x1E))
                base.WritePortFE(addr, value, ref handled);
        }
    }
}
