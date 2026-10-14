using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZXMAK2.Dependency;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Model.Tape.Entities;
using ZXMAK2.Model.Tape.Interfaces;
using ZipLib = ICSharpCode.SharpZipLib;

namespace ZXMAK2.Serializers.TapeSerializers
{
    public class TzxSerializer : FormatSerializer
    {
        private struct SymDef
        {
            public byte SymbolFlags;
            public ushort[] PulseLengths;
        }

        private struct PulseRun
        {
            public byte Symbol;
            public ushort NumberOfRepetitions;
        }

        private readonly ITapeDevice _tape;

        public TzxSerializer(ITapeDevice tape)
        {
            _tape = tape;
        }

        public override string FormatGroup { get { return "Tape images"; } }
        public override string FormatName { get { return "TZX image"; } }
        public override string FormatExtension { get { return "TZX"; } }
        public override bool CanDeserialize { get { return true; } }

        public override void Deserialize(Stream stream)
        {
            _tape.Blocks.Clear();
            var blocks = Load(stream, _tape.TactsPerSecond);
            if (blocks != null)
                _tape.Blocks.AddRange(blocks);
            _tape.Reset();
        }

        private static IEnumerable<ITapeBlock> Load(Stream stream, int frequency)
        {
            if (frequency <= 0)
                frequency = 3500000;

            var snbuf = new byte[stream.Length];
            stream.Read(snbuf, 0, snbuf.Length);
            var signature = Encoding.ASCII.GetString(snbuf, 0, 7);
            if (signature != "ZXTape!" || snbuf[7] != 0x1A)
            {
                Locator.Resolve<IUserMessage>()
                    .Error("TZX loader\n\nInvalid TZX file, identifier not found!");
                return null;
            }

            var tzxBlocks = new List<TapeBlock>();
            var ptr = 10; // skip ZXTape! + 1A + version
            var blockId = -1;
            var encoding = Encoding.Default;
            try
            {
                while (ptr < snbuf.Length)
                {
                    blockId = snbuf[ptr++];
                    var tb = new TapeBlock { Type = (TapeType)blockId };
                    switch (blockId)
                    {
                        case 0x10:
                        {
                            var pause = getUInt16(snbuf, ptr); ptr += 2;
                            var size = getUInt16(snbuf, ptr); ptr += 2;
                            EnsureAvailable(snbuf, ptr, size, size);
                            var data = Slice(snbuf, ptr, size); ptr += size;
                            var pilotLen = size == 0 ? 0 : ((data[0] & 0x80) == 0 ? 8063 : 3223);
                            tb.Pause = pause;
                            tb.Description = "STANDARD " + TapSerializer.getBlockDescription(data, 0, data.Length);
                            tb.Periods = GetBlockPeriods(data, 0, data.Length, 2168, 667, 735, 855, 1710, pilotLen, 8);
                            tb.TapData = data;
                            break;
                        }
                        case 0x11:
                        {
                            var pilotT = getUInt16(snbuf, ptr); ptr += 2;
                            var s1 = getUInt16(snbuf, ptr); ptr += 2;
                            var s2 = getUInt16(snbuf, ptr); ptr += 2;
                            var zeroT = getUInt16(snbuf, ptr); ptr += 2;
                            var oneT = getUInt16(snbuf, ptr); ptr += 2;
                            var pilotLen = getUInt16(snbuf, ptr); ptr += 2;
                            var last = snbuf[ptr++];
                            var pause = getUInt16(snbuf, ptr); ptr += 2;
                            var size = getUInt24(snbuf, ptr); ptr += 3;
                            EnsureAvailable(snbuf, ptr, size, size);
                            var data = Slice(snbuf, ptr, size); ptr += size;
                            tb.Pause = pause;
                            tb.Description = "TURBO " + TapSerializer.getBlockDescription(data, 0, data.Length);
                            tb.Periods = GetBlockPeriods(data, 0, data.Length, pilotT, s1, s2, zeroT, oneT, pilotLen, last);
                            break;
                        }
                        case 0x12:
                        {
                            var pulse = getUInt16(snbuf, ptr); ptr += 2;
                            var count = getUInt16(snbuf, ptr); ptr += 2;
                            tb.Description = "PURE-TONE";
                            tb.Periods = new List<TapePeriod>(count);
                            for (var i = 0; i < count; i++)
                                tb.Periods.Add(new TapePeriod(pulse));
                            break;
                        }
                        case 0x13:
                        {
                            var count = snbuf[ptr++];
                            tb.Description = "PULSES";
                            tb.Periods = new List<TapePeriod>(count);
                            for (var i = 0; i < count; i++, ptr += 2)
                                tb.Periods.Add(new TapePeriod(getUInt16(snbuf, ptr)));
                            break;
                        }
                        case 0x14:
                        {
                            var zeroT = getUInt16(snbuf, ptr); ptr += 2;
                            var oneT = getUInt16(snbuf, ptr); ptr += 2;
                            var last = snbuf[ptr++];
                            var pause = getUInt16(snbuf, ptr); ptr += 2;
                            var size = getUInt24(snbuf, ptr); ptr += 3;
                            EnsureAvailable(snbuf, ptr, size, size);
                            var data = Slice(snbuf, ptr, size); ptr += size;
                            tb.Description = "PURE-DATA";
                            tb.Pause = pause;
                            tb.Periods = GetBlockPeriods(data, 0, data.Length, 0, 0, 0, zeroT, oneT, 0, last);
                            tb.TapData = data;
                            break;
                        }
                        case 0x15:
                            ReadDirectRecording(snbuf, ref ptr, tb);
                            break;
                        case 0x18:
                            ReadCswRecording(snbuf, ref ptr, tb, frequency);
                            break;
                        case 0x19:
                            ReadGeneralizedDataBlock(snbuf, ref ptr, tb);
                            break;
                        case 0x20:
                        {
                            var pause = getUInt16(snbuf, ptr); ptr += 2;
                            tb.Type = TapeType.PAUSE;
                            tb.Pause = pause;
                            tb.Description = pause != 0
                                ? string.Format("PAUSE {0} ms", pause)
                                : "PAUSE 0 ms (STOP)";
                            break;
                        }
                        case 0x21:
                        {
                            var n = snbuf[ptr++];
                            tb.Description = string.Format("GROUP-START \"{0}\"", Stringify(encoding.GetString(snbuf, ptr, n)));
                            ptr += n;
                            break;
                        }
                        case 0x22:
                            tb.Description = "GROUP-END";
                            break;
                        case 0x23:
                        {
                            var offset = getInt16(snbuf, ptr); ptr += 2;
                            tb.Tag = offset;
                            tb.Description = string.Format("JUMP to {0}", tzxBlocks.Count + offset);
                            break;
                        }
                        case 0x24:
                        {
                            var repeats = getUInt16(snbuf, ptr); ptr += 2;
                            tb.Tag = repeats;
                            tb.Description = string.Format("LOOP-START {0}x times", repeats);
                            break;
                        }
                        case 0x25:
                            tb.Description = "LOOP-END";
                            break;
                        case 0x26:
                        {
                            var count = getUInt16(snbuf, ptr); ptr += 2;
                            var offsets = new int[count];
                            for (var i = 0; i < count; i++, ptr += 2)
                                offsets[i] = getInt16(snbuf, ptr);
                            tb.TagArray = offsets;
                            tb.Description = string.Format(
                                "SEQUENCE-CALL {0}",
                                string.Join(",", offsets.Select(o => (tzxBlocks.Count + o).ToString())));
                            break;
                        }
                        case 0x27:
                            tb.Description = "SEQUENCE-RET";
                            break;
                        case 0x28:
                        {
                            var length = getUInt16(snbuf, ptr); ptr += 2;
                            var end = ptr + length;
                            var n = snbuf[ptr++];
                            var parts = new List<string>(n);
                            for (var i = 0; i < n; i++)
                            {
                                var offset = getInt16(snbuf, ptr); ptr += 2;
                                var len = snbuf[ptr++];
                                var text = encoding.GetString(snbuf, ptr, len); ptr += len;
                                parts.Add(string.Format("\"{0}\": {1}", text, tzxBlocks.Count + offset));
                            }
                            if (ptr != end)
                                throw new InvalidDataException("SELECT block size mismatch");
                            tb.Description = "SELECT " + string.Join("; ", parts);
                            break;
                        }
                        case 0x2A:
                        {
                            var extra = getInt32(snbuf, ptr); ptr += 4;
                            ptr += extra & 0x7FFFFFFF;
                            tb.Description = "STOP-48K";
                            break;
                        }
                        case 0x2B:
                        {
                            var extra = getInt32(snbuf, ptr); ptr += 4;
                            tb.Tag = snbuf[ptr];
                            ptr += extra & 0x7FFFFFFF;
                            tb.Description = string.Format("POLARITY {0}", tb.Tag);
                            break;
                        }
                        case 0x30:
                        {
                            var n = snbuf[ptr++];
                            tb.Description = string.Format("INFO \"{0}\"", Stringify(encoding.GetString(snbuf, ptr, n)));
                            ptr += n;
                            break;
                        }
                        case 0x31:
                        {
                            ptr++; // display time
                            var n = snbuf[ptr++];
                            tb.Description = string.Format("MESSAGE \"{0}\"", Stringify(encoding.GetString(snbuf, ptr, n)));
                            ptr += n;
                            break;
                        }
                        case 0x32:
                            tb.Description = ReadArchiveInfo(snbuf, ref ptr, encoding);
                            break;
                        case 0x33:
                        {
                            var n = snbuf[ptr++];
                            ptr += 3 * n;
                            tb.Description = "HARDWARE-INFO";
                            break;
                        }
                        case 0x34:
                            ptr += 8;
                            tb.Description = "EMULATION-INFO";
                            break;
                        case 0x35:
                        {
                            var ident = encoding.GetString(snbuf, ptr, 16); ptr += 16;
                            var size = getInt32(snbuf, ptr); ptr += 4;
                            EnsureAvailable(snbuf, ptr, size, size);
                            ptr += size;
                            tb.Description = string.Format("CUSTOM-INFO \"{0}\"", Stringify(ident));
                            break;
                        }
                        case 0x40:
                        {
                            var kind = snbuf[ptr++];
                            var size = getUInt24(snbuf, ptr); ptr += 3;
                            ptr += size;
                            tb.Description = kind == 0 ? "SNAPSHOT - Z80" :
                                kind == 1 ? "SNAPSHOT - SNA" :
                                string.Format("SNAPSHOT - ??0x{0:x2}??", kind);
                            break;
                        }
                        case 0x5A:
                            ptr += 9;
                            continue;
                        default:
                        {
                            var size = getInt32(snbuf, ptr); ptr += 4;
                            ptr += size & 0x7FFFFFFF;
                            tb.Description = string.Format("[NOT SUPPORTED BLOCK 0x{0:X2}]", (byte)blockId);
                            break;
                        }
                    }
                    tb.Description = string.Format("{0:D3}: #{1:x2} {2}", tzxBlocks.Count, blockId, tb.Description);
                    tzxBlocks.Add(tb);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                var where = blockId >= 0
                    ? string.Format("TZX@{0} ({1:D3}:#{2:x2}): {3}", ptr, tzxBlocks.Count, blockId, ex.Message)
                    : string.Format("TZX@{0} ({1:D3}): {2}", ptr, tzxBlocks.Count, ex.Message);
                Locator.Resolve<IUserMessage>().Warning("{0}: {1}", ex.GetType().Name, where);
            }
            return tzxBlocks;
        }

        private static void ReadDirectRecording(byte[] snbuf, ref int ptr, TapeBlock tb)
        {
            var t0 = getUInt16(snbuf, ptr); ptr += 2;
            var pause = getUInt16(snbuf, ptr); ptr += 2;
            var last = snbuf[ptr++];
            var size = getUInt24(snbuf, ptr); ptr += 3;
            EnsureAvailable(snbuf, ptr, size, size);
            var data = Slice(snbuf, ptr, size); ptr += size;
            tb.Description = "DIRECT-REC";
            tb.Pause = pause;
            if (size == 0)
            {
                tb.Periods = new List<TapePeriod>();
                return;
            }
            var polarity = 0;
            var pulses = 0;
            for (var i = 0; i < size; i++)
            {
                for (var bit = 0x80; bit != 0; bit >>= 1)
                {
                    if (((data[i] ^ polarity) & bit) != 0)
                    {
                        pulses++;
                        polarity ^= -1;
                    }
                }
            }
            polarity = 0;
            var acc = 0;
            tb.Periods = new List<TapePeriod>(pulses + 2);
            for (var i = 0; i < size - 1; i++)
            {
                for (var bit = 0x80; bit != 0; bit >>= 1)
                {
                    acc += t0;
                    if (((data[i] ^ polarity) & bit) != 0)
                    {
                        tb.Periods.Add(new TapePeriod(acc));
                        polarity ^= -1;
                        acc = 0;
                    }
                }
            }
            for (var bit = 0x80; bit != (byte)(0x80 >> last); bit >>= 1)
            {
                acc += t0;
                if (((data[size - 1] ^ polarity) & bit) != 0)
                {
                    tb.Periods.Add(new TapePeriod(acc));
                    polarity ^= -1;
                    acc = 0;
                }
            }
            if (acc > 0)
                tb.Periods.Add(new TapePeriod(acc));
        }

        private static void ReadCswRecording(byte[] snbuf, ref int ptr, TapeBlock tb, int frequency)
        {
            var blockLen = getInt32(snbuf, ptr); ptr += 4;
            var end = ptr + blockLen;
            var pause = getUInt16(snbuf, ptr); ptr += 2;
            var sampleRate = getUInt24(snbuf, ptr); ptr += 3;
            var compression = snbuf[ptr++];
            var pulseCount = getInt32(snbuf, ptr); ptr += 4;
            var dataSize = (int)((blockLen - 10) & 0x7FFFFFFF);
            EnsureAvailable(snbuf, ptr, dataSize, dataSize);
            var pulses = ReadCswRle(snbuf, ptr, dataSize, compression, (uint)pulseCount);
            ptr += dataSize;
            if (ptr != end)
                throw new InvalidDataException("CSW block size mismatch");
            tb.Description = "CSW";
            tb.Pause = pause;
            var ratio = (double)frequency / sampleRate;
            tb.Periods = pulses
                .Select(p => (int)Math.Round(p * ratio, MidpointRounding.AwayFromZero))
                .Select(t => new TapePeriod(t))
                .ToList();
        }

        private static IEnumerable<uint> ReadCswRle(byte[] buffer, int offset, int length, byte compression, uint pulseCount)
        {
            if (compression != 1 && compression != 2)
                throw new NotSupportedException(string.Format("Not supported CSW compression: 0x{0:x2}", compression));

            using (var memory = new MemoryStream(buffer, offset, length))
            {
                Stream stream = memory;
                if (compression == 2)
                    stream = new ZipLib.Zip.Compression.Streams.InflaterInputStream(memory);
                var list = new List<uint>();
                var buf = new byte[4];
                for (var i = 0; i < pulseCount; i++)
                {
                    Array.Clear(buf, 0, 4);
                    if (stream.Read(buf, 0, 1) != 1 ||
                        (buf[0] == 0 && stream.Read(buf, 0, 4) != 4))
                    {
                        Logger.Warn("CSW: Unexpected end of stream @ {0}", stream.Position);
                        break;
                    }
                    list.Add((uint)getInt32(buf, 0));
                }
                if (compression == 2)
                    stream.Dispose();
                return list;
            }
        }

        private static void ReadGeneralizedDataBlock(byte[] snbuf, ref int ptr, TapeBlock tb)
        {
            var blockLen = getInt32(snbuf, ptr); ptr += 4;
            var end = ptr + blockLen;
            var pause = getUInt16(snbuf, ptr); ptr += 2;
            var totp = getInt32(snbuf, ptr); ptr += 4;
            var npp = snbuf[ptr++];
            int asp = snbuf[ptr++];
            var totd = getInt32(snbuf, ptr); ptr += 4;
            var npd = snbuf[ptr++];
            int asd = snbuf[ptr++];
            if (asp == 0) asp = 256;
            if (asd == 0) asd = 256;

            var pilotSym = ReadSymDefs(snbuf, ref ptr, asp, npp);
            var pilot = new PulseRun[totp];
            for (var i = 0; i < totp; i++)
            {
                pilot[i].Symbol = snbuf[ptr++];
                pilot[i].NumberOfRepetitions = getUInt16(snbuf, ptr); ptr += 2;
            }
            var dataSym = ReadSymDefs(snbuf, ref ptr, asd, npd);
            var bitsPerSymbol = (int)Math.Ceiling(Math.Log(asd, 2.0));
            var dataBytes = (int)Math.Ceiling((bitsPerSymbol * (double)totd) / 8.0);
            EnsureAvailable(snbuf, ptr, dataBytes, dataBytes);
            var data = Slice(snbuf, ptr, dataBytes); ptr += dataBytes;
            if (ptr != end)
                throw new InvalidDataException("GENERALIZED block size mismatch");

            tb.Description = string.Format("GENERALIZED asd:{0}, nb:{1}", asd, bitsPerSymbol);
            tb.Pause = pause;
            tb.Periods = new List<TapePeriod>();
            AppendSymbols(tb.Periods, pilotSym, pilot);
            if (bitsPerSymbol == 3 || bitsPerSymbol == 5 || bitsPerSymbol == 6 || bitsPerSymbol == 7 || bitsPerSymbol > 8)
                Logger.Warn("invalid number of data symbols: asd={0}, nb={1}", asd, bitsPerSymbol);
            if (bitsPerSymbol <= 0)
                return;
            var mask = (uint)((1 << bitsPerSymbol) - 1);
            for (var i = 0; i < data.Length; i++)
            {
                var b = data[i];
                var steps = 8 / bitsPerSymbol;
                for (var s = 0; s < steps; s++)
                {
                    b = (byte)((b << bitsPerSymbol) | (b >> (8 - bitsPerSymbol)));
                    var symbol = b & mask;
                    AppendSymbol(tb.Periods, dataSym[symbol]);
                }
            }
        }

        private static SymDef[] ReadSymDefs(byte[] snbuf, ref int ptr, int count, int pulses)
        {
            var defs = new SymDef[count];
            for (var i = 0; i < count; i++)
            {
                defs[i].SymbolFlags = snbuf[ptr++];
                defs[i].PulseLengths = new ushort[pulses];
                for (var p = 0; p < pulses; p++, ptr += 2)
                    defs[i].PulseLengths[p] = getUInt16(snbuf, ptr);
            }
            return defs;
        }

        private static void AppendSymbols(List<TapePeriod> dest, SymDef[] table, PulseRun[] runs)
        {
            foreach (var run in runs)
            {
                for (var r = 0; r < run.NumberOfRepetitions; r++)
                    AppendSymbol(dest, table[run.Symbol]);
            }
        }

        private static void AppendSymbol(List<TapePeriod> dest, SymDef def)
        {
            for (var i = 0; i < def.PulseLengths.Length && def.PulseLengths[i] != 0; i++)
            {
                var flags = (TapeFlags)0;
                if (i == 0)
                {
                    switch (def.SymbolFlags & 3)
                    {
                        case 1: flags = TapeFlags.NO_EDGE; break;
                        case 2: flags = TapeFlags.LEVEL_LOW; break;
                        case 3: flags = TapeFlags.LEVEL_HIGH; break;
                    }
                }
                dest.Add(new TapePeriod(def.PulseLengths[i], flags));
            }
        }

        private static string ReadArchiveInfo(byte[] snbuf, ref int ptr, Encoding encoding)
        {
            var length = getUInt16(snbuf, ptr); ptr += 2;
            var end = ptr + length;
            var n = snbuf[ptr++];
            var parts = new List<string>();
            for (var i = 0; i < n; i++)
            {
                var type = snbuf[ptr++];
                var len = snbuf[ptr++];
                var value = encoding.GetString(snbuf, ptr, len); ptr += len;
                if (type == 0 || type == 7 || type == 0xFF)
                    parts.Add(string.Format("{0}: {1}", ArchiveType(type), Stringify(value)));
            }
            if (ptr != end)
                throw new InvalidDataException("ARCHIVE-INFO size mismatch");
            return "ARCHIVE-INFO " + string.Join(";", parts);
        }

        private static string ArchiveType(byte type)
        {
            switch (type)
            {
                case 0: return "Full title";
                case 1: return "Publisher";
                case 2: return "Author";
                case 3: return "Year";
                case 4: return "Language";
                case 5: return "Type";
                case 6: return "Price";
                case 7: return "Protection";
                case 8: return "Origin";
                case 0xFF: return "Comment";
                default: return "Unknown";
            }
        }

        public static List<TapePeriod> GetBlockPeriods(
            byte[] block, int indexOffset, int blockLength,
            int pilotT, int s1T, int s2T, int zeroT, int oneT,
            int pilotLen, int last)
        {
            var periods = new List<TapePeriod>();
            if (pilotLen > 0)
            {
                for (var i = 0; i < pilotLen; i++)
                    periods.Add(new TapePeriod(pilotT));
                periods.Add(new TapePeriod(s1T));
                periods.Add(new TapePeriod(s2T));
            }
            if (blockLength <= 0)
                return periods;
            var src = indexOffset;
            for (var i = 0; i < blockLength - 1; i++, src++)
            {
                for (var bit = 0x80; bit != 0; bit >>= 1)
                    AddBit(periods, (block[src] & bit) != 0, zeroT, oneT);
            }
            for (var bit = 0x80; bit != (byte)(0x80 >> last); bit >>= 1)
                AddBit(periods, (block[src] & bit) != 0, zeroT, oneT);
            return periods;
        }

        private static void AddBit(List<TapePeriod> periods, bool one, int zeroT, int oneT)
        {
            var tstates = one ? oneT : zeroT;
            var flags = one ? TapeFlags.LENGTH_LONG : TapeFlags.LENGTH_SHORT;
            periods.Add(new TapePeriod(tstates, flags));
            periods.Add(new TapePeriod(tstates, flags));
        }

        private static byte[] Slice(byte[] src, int offset, int size)
        {
            var data = new byte[size];
            Array.Copy(src, offset, data, 0, size);
            return data;
        }

        private static void EnsureAvailable(byte[] buf, int ptr, int size, int reported)
        {
            if (size < 0 || ptr + size > buf.Length)
                throw new InvalidDataException(string.Format("unexpected size={0} (available {1})", reported, buf.Length - ptr));
        }

        private static string Stringify(string text)
        {
            return text.Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
        }
    }
}
