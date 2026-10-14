using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Model.Tape.Entities;
using ZXMAK2.Model.Tape.Interfaces;

namespace ZXMAK2.Serializers.TapeSerializers
{
    public class TapSerializer : FormatSerializer
    {
        private readonly ITapeDevice _tape;

        public TapSerializer(ITapeDevice tape)
        {
            _tape = tape;
        }

        public override string FormatGroup { get { return "Tape images"; } }
        public override string FormatName { get { return "TAP image"; } }
        public override string FormatExtension { get { return "TAP"; } }
        public override bool CanDeserialize { get { return true; } }

        public override void Deserialize(Stream stream)
        {
            _tape.Blocks.Clear();
            var blocks = Load(stream);
            if (blocks != null)
                _tape.Blocks.AddRange(blocks);
            _tape.Reset();
        }

        private static IEnumerable<ITapeBlock> Load(Stream stream)
        {
            var list = new List<TapeBlock>();
            var bsize = new byte[2];
            while (stream.Position < stream.Length)
            {
                stream.Read(bsize, 0, 2);
                var size = BitConverter.ToUInt16(bsize, 0);
                if (size == 0)
                    break;
                var block = new byte[size];
                stream.Read(block, 0, size);
                list.Add(new TapeBlock
                {
                    Type = TapeType.ROM,
                    Description = getBlockDescription(block, 0, block.Length),
                    Periods = TzxSerializer.GetBlockPeriods(
                        block, 0, block.Length, 2168, 667, 735, 855, 1710,
                        block[0] < 4 ? 8064 : 3220, 8),
                    Pause = 1000,
                    TapData = block,
                });
            }
            return list;
        }

        public static string getBlockDescription(byte[] block, int indexOffset, int blockLength)
        {
            if (block == null || blockLength <= 0)
                return "empty block";
            byte crc = 0;
            byte crcValue = 0;
            byte crcFile = 0;
            var prg = new byte[10];
            for (var i = 0; i < blockLength; i++)
            {
                crc ^= block[indexOffset + i];
                if (i < blockLength - 1)
                    crcValue = crc;
                else
                    crcFile = block[indexOffset + i];
            }

            string dst;
            if (block[indexOffset] == 0 && blockLength == 19 &&
                (block[indexOffset + 1] == 0 || block[indexOffset + 1] == 3))
            {
                for (var i = 0; i < 10; i++)
                {
                    var ch = block[indexOffset + i + 2];
                    prg[i] = (ch < 0x20 || ch >= 0x80) ? (byte)0x3F : ch;
                }
                var sprg = Encoding.ASCII.GetString(prg, 0, 10);
                var styp = block[indexOffset + 1] != 0 ? "Bytes" : "Program";
                dst = string.Format("{0}: \"{1}\" {2},{3}", styp, sprg,
                    getUInt16(block, indexOffset + 14),
                    getUInt16(block, indexOffset + 12));
            }
            else if (block[indexOffset] == 0xFF)
                dst = string.Format("Data block, {0} bytes", blockLength - 2);
            else
                dst = string.Format("#{0} block, {1} bytes", block[indexOffset].ToString("X2"), blockLength - 2);
            dst += string.Format(", crc {0}",
                crc != 0
                    ? string.Format("bad (#{0:X2}!=#{1:X2})", crcFile, crcValue)
                    : "ok");
            return dst;
        }
    }
}
