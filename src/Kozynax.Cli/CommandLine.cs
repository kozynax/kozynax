using System;
using System.Collections.Generic;
using System.Globalization;
using Kozynax.Cli.Services;
using Kozynax.Cli.Utils;
using ZXMAK2;
using ZXMAK2.Dependency;
using ZXMAK2.Host.Interfaces;

namespace Kozynax.Cli
{
    /// <summary>
    /// Headless CLI commands and shared host argument parsing.
    /// </summary>
    public static class CommandLine
    {
        private const string AppName = "kozynax";

        /// <summary>
        /// For WinExe hosts: attach/allocate a console when args need stdout
        /// (<c>convert</c>, <c>--tui</c>, or <c>--console</c>).
        /// No-op for normal GUI launches and on non-Windows.
        /// </summary>
        public static void EnsureConsoleIfNeeded(string[] args)
        {
            if (!NeedsConsole(args))
                return;
            WindowsConsole.EnsureAttached(exclusiveInput: NeedsExclusiveConsole(args));
        }

        /// <summary>
        /// Handles known commands such as <c>convert</c>.
        /// Returns true when <paramref name="args"/> was a CLI command (caller should exit).
        /// </summary>
        public static bool TryHandle(string[] args, out int exitCode)
        {
            exitCode = 0;
            if (args == null || args.Length == 0)
                return false;

            if (!string.Equals(args[0], "convert", StringComparison.OrdinalIgnoreCase))
                return false;

            exitCode = RunConvert(args);
            return true;
        }

        private static bool NeedsConsole(string[] args)
        {
            if (args == null || args.Length == 0)
                return false;

            if (string.Equals(args[0], "convert", StringComparison.OrdinalIgnoreCase))
                return true;

            foreach (var arg in args)
            {
                if (string.Equals(arg, "--tui", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(arg, "--console", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static bool NeedsExclusiveConsole(string[] args)
        {
            if (args == null)
                return false;
            foreach (var arg in args)
            {
                if (string.Equals(arg, "--tui", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(arg, "--console", StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Parses host-only flags (e.g. <c>--tui</c>, <c>--console</c>) and returns the remaining launcher args.
        /// </summary>
        public static HostLaunchOptions ParseHostOptions(string[] args)
        {
            var useTui = false;
            int? windowScaleRatio = null;
            if (args == null || args.Length == 0)
                return new HostLaunchOptions(useTui, Array.Empty<string>(), windowScaleRatio);

            var filtered = new List<string>(args.Length);
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (string.Equals(arg, "--tui", StringComparison.OrdinalIgnoreCase))
                {
                    useTui = true;
                    continue;
                }
                if (string.Equals(arg, "--console", StringComparison.OrdinalIgnoreCase))
                    continue;

                string scaleValue = null;
                if (arg.StartsWith("--scale=", StringComparison.OrdinalIgnoreCase))
                    scaleValue = arg.Substring("--scale=".Length);
                else if (string.Equals(arg, "--scale", StringComparison.OrdinalIgnoreCase)
                         && i + 1 < args.Length)
                {
                    scaleValue = args[++i];
                }

                if (scaleValue != null)
                {
                    if (TryParseWindowScale(scaleValue, out var scale))
                        windowScaleRatio = scale;
                    continue;
                }

                filtered.Add(arg);
            }
            return new HostLaunchOptions(useTui, filtered.ToArray(), windowScaleRatio);
        }

        /// <summary>
        /// Parses a window scale factor. The fractional part is dropped (125% → 1).
        /// Accepts <c>100%</c>, <c>1.25</c>, or a bare percent <c>100</c> (≥ 10).
        /// </summary>
        public static bool TryParseWindowScale(string text, out int scale)
        {
            scale = 1;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            text = text.Trim();
            var percent = text.EndsWith("%", StringComparison.Ordinal);
            if (percent)
                text = text.Substring(0, text.Length - 1).Trim();

            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value)
                || value < 0
                || double.IsNaN(value)
                || double.IsInfinity(value))
            {
                return false;
            }

            double factor;
            if (percent || value >= 10)
                factor = value / 100.0;
            else
                factor = value;

            scale = Math.Max(1, (int)Math.Truncate(factor));
            return true;
        }

        private static int RunConvert(string[] args)
        {
            if (args.Length != 3)
            {
                PrintConvertUsage();
                return 1;
            }

            var sourcePath = args[1];
            var destPath = args[2];

            var messages = new ConsoleUserMessage();
            var resolver = new ResolverSimple();
            resolver.RegisterInstance<IResolver>(resolver);
            resolver.RegisterInstance<IUserMessage>(messages);
            resolver.RegisterInstance<IUserQuery>(new ConsoleUserQuery());

            Locator.Init(resolver);
            try
            {
                var code = DiskImageConverter.Convert(sourcePath, destPath, messages);
                if (messages.HadError && code == 0)
                    return 1;
                return code;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                Logger.Error(ex);
                return 1;
            }
            finally
            {
                Locator.Shutdown();
            }
        }

        private static void PrintConvertUsage()
        {
            Console.Error.WriteLine($"Usage: {AppName} convert <input> <output>");
            Console.Error.WriteLine();
            Console.Error.WriteLine("Convert a disk image between supported formats, e.g.:");
            Console.Error.WriteLine($"  {AppName} convert image.trd image.hfe");
            Console.Error.WriteLine($"  {AppName} convert image.scl image.udi");
        }
    }

    public sealed class HostLaunchOptions
    {
        public HostLaunchOptions(bool useTui, string[] args, int? windowScaleRatio = null)
        {
            UseTui = useTui;
            Args = args ?? Array.Empty<string>();
            WindowScaleRatio = windowScaleRatio;
        }

        /// <summary>When true, the SDL host should use <c>StdioTerminal</c>.</summary>
        public bool UseTui { get; }

        /// <summary>
        /// Integer window scale from <c>--scale</c> (100% → 1, 250% → 2). Null if not set.
        /// </summary>
        public int? WindowScaleRatio { get; }

        /// <summary>Args with host-only flags removed, suitable for <c>ILauncher.Run</c>.</summary>
        public string[] Args { get; }
    }
}
