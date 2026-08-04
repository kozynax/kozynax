using System;
using ZXMAK2.Dependency;
using ZXMAK2.Engine.Interfaces;
using ZXMAK2.Hardware.Circuits.Sound;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.Presentation;
using ZXMAK2.Host.Presentation.Interfaces;
using ZXMAK2.Host.SdlBackend;
using ZXMAK2.Host.SdlBackend.Services;

namespace ZXMAK2
{
    public static class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException +=
                    (s, e) => Logger.Fatal(e.ExceptionObject as Exception, "AppDomain.UnhandledException");
                RunSafe(args);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                Console.Error.WriteLine(ex);
            }
        }

        private static void RunSafe(string[] args)
        {
            var resolver = new ResolverSimple();
            var sdl = Silk.NET.SDL.Sdl.GetApi();

            resolver.RegisterInstance<IResolver>(resolver);
            resolver.RegisterInstance(sdl);
            resolver.RegisterType<ISettingService, SdlSettingService>(true);
            resolver.RegisterType<IUserMessage, SdlUserMessage>();
            resolver.RegisterType<IUserQuery, SdlUserQuery>();
            resolver.RegisterType<IUserHelp, SdlUserHelp>();
            resolver.RegisterType<IOpenFileDialog, SdlOpenFileDialog>();
            resolver.RegisterType<ISaveFileDialog, SdlSaveFileDialog>();

            resolver.RegisterType<IMainView, SdlMainView>();
            resolver.RegisterType<ILauncher, Launcher>(true);
            resolver.RegisterType<IMainViewModel, MainViewModel>();
            resolver.RegisterType<IPsgChip, PsgChip>();

            // WinForms dialogs are unavailable in the SDL shell.
            // Settings/debugger commands stay disabled until Kozui hosts are ported.

            // Ensure Wayland is chosen before any SDL_Init (SdlMainView also sets this).
            if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SDL_VIDEODRIVER"))
                && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")))
            {
                Environment.SetEnvironmentVariable("SDL_VIDEODRIVER", "wayland");
            }

            Locator.Init(resolver);
            var launcher = Locator.Resolve<ILauncher>();
            launcher.Run(args);
            Locator.Shutdown();
        }
    }
}
