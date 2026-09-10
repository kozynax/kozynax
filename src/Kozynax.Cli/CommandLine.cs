using System;
using System.Collections.Generic;
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

        /// <summary>
        /// Parses host-only flags (e.g. <c>--tui</c>) and returns the remaining launcher args.
        /// </summary>
        public static HostLaunchOptions ParseHostOptions(string[] args)
        {
            var useTui = false;
            if (args == null || args.Length == 0)
                return new HostLaunchOptions(useTui, Array.Empty<string>());

            var filtered = new List<string>(args.Length);
            foreach (var arg in args)
            {
                if (string.Equals(arg, "--tui", StringComparison.OrdinalIgnoreCase))
                {
                    useTui = true;
                    continue;
                }
                filtered.Add(arg);
            }
            return new HostLaunchOptions(useTui, filtered.ToArray());
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
        public HostLaunchOptions(bool useTui, string[] args)
        {
            UseTui = useTui;
            Args = args ?? Array.Empty<string>();
        }

        /// <summary>When true, the SDL host should use <c>StdioTerminal</c>.</summary>
        public bool UseTui { get; }

        /// <summary>Args with host-only flags removed, suitable for <c>ILauncher.Run</c>.</summary>
        public string[] Args { get; }
    }
}
