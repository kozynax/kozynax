using System;
using System.Collections.Generic;
using System.IO;
using ZXMAK2.Engine;
using ZipLib = ICSharpCode.SharpZipLib;

namespace ZXMAK2.Host.Terminal
{
    /// <summary>
    /// Terminal bitmap font: Spectrum Latin (48.rom @ 0x3D00) plus Quorum Cyrillic
    /// (QU4I1993.ROM @ 0x3B00 / 0x3C00, KOI8-R letter order).
    /// Bit 7 is the leftmost pixel (ZX convention).
    /// </summary>
    public static class TerminalFont
    {
        public const int GlyphWidth = 8;
        public const int GlyphHeight = 8;

        private const int LatinRomOffset = 0x3D00;
        private const int LatinGlyphCount = 96; // 0x20..0x7F
        private const int LatinByteCount = LatinGlyphCount * GlyphHeight;

        private const int CyrillicRomOffset = 0x3B00;
        private const int CyrillicGlyphCount = 64; // 32 upper + 32 lower (KOI8-R order)
        private const int CyrillicByteCount = CyrillicGlyphCount * GlyphHeight;

        /// <summary>
        /// Glyph order in QU4I1993.ROM: KOI8-R 0xE0..0xFF then 0xC0..0xDF.
        /// </summary>
        private const string CyrillicOrder =
            "ЮАБЦДЕФГХИЙКЛМНОПЯРСТУЖВЬЫЗШЭЩЧЪ" +
            "юабцдефгхийклмнопярстужвьызшэщчъ";

        private static readonly byte[] LatinGlyphs = LoadLatinFont();
        private static readonly byte[] CyrillicGlyphs = LoadCyrillicFont();
        private static readonly Dictionary<char, int> CyrillicIndex = BuildCyrillicIndex();

        public static int MeasureWidth(string text, int scale)
            => (text?.Length ?? 0) * GlyphWidth * Math.Max(scale, 1);

        public static void Draw(ITerminal terminal, int x, int y, string text, int scale, TerminalColor color)
        {
            if (terminal == null || string.IsNullOrEmpty(text) || scale < 1)
                return;

            Draw((px, py, w, h, c) => terminal.FillRect(px, py, w, h, c), x, y, text, scale, color);
        }

        /// <summary>Draw glyphs via an arbitrary fill callback (e.g. SDL renderer).</summary>
        public static void Draw(
            Action<int, int, int, int, TerminalColor> fillRect,
            int x,
            int y,
            string text,
            int scale,
            TerminalColor color)
        {
            if (fillRect == null || string.IsNullOrEmpty(text) || scale < 1)
                return;

            var cx = x;
            foreach (var ch in text)
            {
                DrawGlyph(fillRect, ch, cx, y, scale, color);
                cx += GlyphWidth * scale;
            }
        }

        private static void DrawGlyph(
            Action<int, int, int, int, TerminalColor> fillRect,
            char ch,
            int x,
            int y,
            int scale,
            TerminalColor color)
        {
            byte[] glyphs;
            int index;
            if (ch >= 32 && ch <= 126)
            {
                glyphs = LatinGlyphs;
                index = ch - 32;
            }
            else if (CyrillicIndex.TryGetValue(ch, out index))
            {
                glyphs = CyrillicGlyphs;
            }
            else
            {
                glyphs = LatinGlyphs;
                index = '?' - 32;
            }

            var offset = index * GlyphHeight;
            for (var row = 0; row < GlyphHeight; row++)
            {
                var bits = glyphs[offset + row];
                for (var col = 0; col < GlyphWidth; col++)
                {
                    // ZX Spectrum: bit 7 is leftmost
                    if ((bits & (0x80 >> col)) == 0)
                        continue;
                    fillRect(x + col * scale, y + row * scale, scale, scale, color);
                }
            }
        }

        private static Dictionary<char, int> BuildCyrillicIndex()
        {
            var map = new Dictionary<char, int>(CyrillicOrder.Length + 2);
            for (var i = 0; i < CyrillicOrder.Length; i++)
                map[CyrillicOrder[i]] = i;

            // Ё/ё are not in the Quorum 32-letter block; reuse Е/е.
            if (map.TryGetValue('Е', out var ye))
                map['Ё'] = ye;
            if (map.TryGetValue('е', out var yeSmall))
                map['ё'] = yeSmall;

            return map;
        }

        private static byte[] LoadLatinFont()
        {
            try
            {
                using (var rom = OpenRomStream("ZX048/48.rom"))
                {
                    if (rom.Length < LatinRomOffset + LatinByteCount)
                        throw new InvalidDataException("48.rom is too small for charset at 0x3D00");

                    rom.Position = LatinRomOffset;
                    var font = new byte[LatinByteCount];
                    var read = rom.Read(font, 0, font.Length);
                    if (read != font.Length)
                        throw new EndOfStreamException("Failed to read Spectrum charset");
                    return font;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load Spectrum font from 48.rom; using blank glyphs");
                return new byte[LatinByteCount];
            }
        }

        private static byte[] LoadCyrillicFont()
        {
            try
            {
                using (var rom = OpenRomStream("QUORUM/QU4I1993.ROM"))
                {
                    if (rom.Length < CyrillicRomOffset + CyrillicByteCount)
                        throw new InvalidDataException("QU4I1993.ROM is too small for Cyrillic font at 0x3B00");

                    rom.Position = CyrillicRomOffset;
                    var font = new byte[CyrillicByteCount];
                    var read = rom.Read(font, 0, font.Length);
                    if (read != font.Length)
                        throw new EndOfStreamException("Failed to read Quorum Cyrillic charset");
                    return font;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load Cyrillic font from QU4I1993.ROM; Cyrillic will show as '?'");
                return new byte[CyrillicByteCount];
            }
        }

        private static Stream OpenRomStream(string imageName)
        {
            var folderName = Utils.GetAppFolder();

            var romsFolderName = Path.Combine(folderName, "roms");
            if (Directory.Exists(romsFolderName))
            {
                var romsFileName = Path.Combine(romsFolderName, imageName.Replace('/', Path.DirectorySeparatorChar));
                if (File.Exists(romsFileName))
                    return File.OpenRead(romsFileName);

                // allow plain 48.rom drop-in
                var plain = Path.Combine(romsFolderName, "48.rom");
                if (File.Exists(plain) && imageName.EndsWith("48.rom", StringComparison.OrdinalIgnoreCase))
                    return File.OpenRead(plain);
            }

            var pakFileName = Path.Combine(folderName, "ROMS.PAK");
            using (var zip = new ZipLib.Zip.ZipFile(pakFileName))
            {
                foreach (ZipLib.Zip.ZipEntry entry in zip)
                {
                    if (!entry.IsFile || !entry.CanDecompress)
                        continue;
                    if (string.Compare(entry.Name, imageName, StringComparison.OrdinalIgnoreCase) != 0)
                        continue;

                    using (var input = zip.GetInputStream(entry))
                    {
                        var data = new byte[entry.Size];
                        var offset = 0;
                        while (offset < data.Length)
                        {
                            var n = input.Read(data, offset, data.Length - offset);
                            if (n <= 0)
                                break;
                            offset += n;
                        }
                        return new MemoryStream(data, false);
                    }
                }
            }

            throw new FileNotFoundException("ROM file not found: " + imageName);
        }
    }
}
