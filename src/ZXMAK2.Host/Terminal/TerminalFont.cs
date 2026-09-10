using System;
using System.IO;
using ZXMAK2.Engine;
using ZipLib = ICSharpCode.SharpZipLib;

namespace ZXMAK2.Host.Terminal
{
    /// <summary>
    /// Spectrum 48K ROM character set (8x8, ASCII 0x20..0x7F) from 48.rom @ 0x3D00.
    /// Bit 7 is the leftmost pixel (ZX convention).
    /// </summary>
    public static class TerminalFont
    {
        public const int GlyphWidth = 8;
        public const int GlyphHeight = 8;
        private const int RomFontOffset = 0x3D00;
        private const int GlyphCount = 96; // 0x20..0x7F
        private const int FontByteCount = GlyphCount * GlyphHeight;

        private static readonly byte[] Glyphs = LoadSpectrumFont();

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
            var index = ch < 32 || ch > 126 ? '?' - 32 : ch - 32;
            var offset = index * GlyphHeight;
            for (var row = 0; row < GlyphHeight; row++)
            {
                var bits = Glyphs[offset + row];
                for (var col = 0; col < GlyphWidth; col++)
                {
                    // ZX Spectrum: bit 7 is leftmost
                    if ((bits & (0x80 >> col)) == 0)
                        continue;
                    fillRect(x + col * scale, y + row * scale, scale, scale, color);
                }
            }
        }

        private static byte[] LoadSpectrumFont()
        {
            try
            {
                using (var rom = OpenRomStream("ZX048/48.rom"))
                {
                    if (rom.Length < RomFontOffset + FontByteCount)
                        throw new InvalidDataException("48.rom is too small for charset at 0x3D00");

                    rom.Position = RomFontOffset;
                    var font = new byte[FontByteCount];
                    var read = rom.Read(font, 0, font.Length);
                    if (read != font.Length)
                        throw new EndOfStreamException("Failed to read Spectrum charset");
                    return font;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Failed to load Spectrum font from 48.rom; using blank glyphs");
                return new byte[FontByteCount];
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
