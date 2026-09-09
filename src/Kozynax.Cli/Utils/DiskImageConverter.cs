using System;
using System.IO;
using System.Linq;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Model.Disk;
using ZXMAK2.Serializers;

namespace Kozynax.Cli.Utils
{
    /// <summary>
    /// Loads a disk image and writes it in another supported format.
    /// </summary>
    public static class DiskImageConverter
    {
        // Same timing as Wd1793 (Z80FQ / FDD_RPS).
        private const long RotateTime = 3500000 / 5;

        public static int Convert(string sourcePath, string destPath, IUserMessage messages = null)
        {
            if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(destPath))
            {
                WriteError(messages, "Source and destination paths are required.");
                return 1;
            }

            sourcePath = Path.GetFullPath(sourcePath);
            destPath = Path.GetFullPath(destPath);

            if (!File.Exists(sourcePath))
            {
                WriteError(messages, "Source file not found: {0}", sourcePath);
                return 1;
            }

            var disk = new DiskImage();
            disk.Init(RotateTime);
            var manager = new DiskLoadManager(disk);

            if (!manager.CheckCanOpenFileName(sourcePath))
            {
                WriteError(
                    messages,
                    "Unsupported input format: {0}\n\nOpenable: {1}",
                    Path.GetExtension(sourcePath),
                    FormatList(manager, openable: true));
                return 1;
            }

            if (!manager.CheckCanSaveFileName(destPath))
            {
                WriteError(
                    messages,
                    "Unsupported output format: {0}\n\nWritable: {1}",
                    Path.GetExtension(destPath),
                    FormatList(manager, openable: false));
                return 1;
            }

            manager.OpenFileName(sourcePath, true);
            if (!disk.Present)
            {
                WriteError(messages, "Failed to load: {0}", sourcePath);
                return 1;
            }

            var destDir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(destDir) && !Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);

            manager.SaveFileName(destPath);

            if (!File.Exists(destPath) || new FileInfo(destPath).Length == 0)
            {
                WriteError(messages, "Failed to write: {0}", destPath);
                return 1;
            }

            Console.WriteLine("Converted {0} -> {1}", sourcePath, destPath);
            return 0;
        }

        private static string FormatList(DiskLoadManager manager, bool openable)
        {
            var exts = manager.GetSerializers()
                .Where(s => openable ? s.CanDeserialize : s.CanSerialize)
                .Select(s => s.FormatExtension == "$" ? ".$ / .!" : "." + s.FormatExtension.ToLowerInvariant())
                .Distinct()
                .OrderBy(s => s);
            return string.Join(", ", exts);
        }

        private static void WriteError(IUserMessage messages, string fmt, params object[] args)
        {
            var text = args != null && args.Length > 0 ? string.Format(fmt, args) : fmt;
            if (messages != null)
                messages.Error("{0}", text);
            else
                Console.Error.WriteLine(text);
        }
    }
}
