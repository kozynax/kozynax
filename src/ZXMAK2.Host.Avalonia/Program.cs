using System;
using Kozui.Interfaces;
using Kozynax.Cli;
using Kozynax.UI;
using ZXMAK2.Dependency;
using ZXMAK2.Hardware.Circuits.Sound;
using ZXMAK2.Host.AvaloniaBackend;
using ZXMAK2.Host.AvaloniaBackend.Services;
using ZXMAK2.Host.AvaloniaBackend.Views;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.Presentation;
using ZXMAK2.Host.Presentation.Interfaces;
using ZXMAK2.Host.SdlBackend.Views;

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
            CommandLine.EnsureConsoleIfNeeded(args);

            if (CommandLine.TryHandle(args, out var exitCode))
            {
                Environment.ExitCode = exitCode;
                return;
            }

            var options = CommandLine.ParseHostOptions(args);

            var resolver = new ResolverSimple();
            var sdl = Silk.NET.SDL.Sdl.GetApi();
            var runtime = new AvaloniaRuntimeContext();

            resolver.RegisterInstance<IResolver>(resolver);
            resolver.RegisterInstance(sdl);
            resolver.RegisterInstance(runtime);
            resolver.RegisterInstance(options);
            resolver.RegisterType<ISettingService, AvaloniaSettingService>(true);
            resolver.RegisterType<IUserMessage, AvaloniaUserMessage>();
            resolver.RegisterType<IUserQuery, AvaloniaUserQuery>();
            resolver.RegisterType<IUserHelp, AvaloniaUserHelp>();
            resolver.RegisterType<IOpenFileDialog, AvaloniaOpenFileDialog>();
            resolver.RegisterType<ISaveFileDialog, AvaloniaSaveFileDialog>();

            // Kozui dialogs: placeholder hosts until the Kozui -> Avalonia presenter lands.
            // Tool-view adapters are shared with the SDL host (host-neutral).
            resolver.RegisterType<IViewImplementation<PreferencesDialog>, PendingKozuiDialogHost<PreferencesDialog>>();
            resolver.RegisterType<IViewImplementation<ConfirmDialog>, PendingKozuiDialogHost<ConfirmDialog>>();
            resolver.RegisterType<IViewImplementation<InputDialog>, PendingKozuiDialogHost<InputDialog>>();
            resolver.RegisterType<IViewImplementation<ObjectSelectorDialog>, PendingKozuiDialogHost<ObjectSelectorDialog>>();
            resolver.RegisterType<IViewImplementation<FilePickerDialog>, PendingKozuiDialogHost<FilePickerDialog>>();
            resolver.RegisterType<IViewImplementation<AboutDialog>, PendingKozuiDialogHost<AboutDialog>>();
            resolver.RegisterType<IViewImplementation<KeyboardHelpDialog>, PendingKozuiDialogHost<KeyboardHelpDialog>>();
            resolver.RegisterType<IViewImplementation<MemoryMap>, PendingKozuiDialogHost<MemoryMap>>();
            resolver.RegisterType<IViewImplementation<FddDebugDialog>, PendingKozuiDialogHost<FddDebugDialog>>();
            resolver.RegisterType<IViewImplementation<TapeSettings>, PendingKozuiDialogHost<TapeSettings>>();
            resolver.RegisterType<IViewImplementation<MachineSettings>, PendingKozuiDialogHost<MachineSettings>>();
            resolver.RegisterType<IViewImplementation<AddDeviceDialog>, PendingKozuiDialogHost<AddDeviceDialog>>();
            resolver.RegisterType<IViewImplementation<DebuggerDialog>, PendingKozuiDialogHost<DebuggerDialog>>();
            resolver.RegisterType<IViewImplementation<SprinterDebuggerDialog>, PendingKozuiDialogHost<SprinterDebuggerDialog>>();
            resolver.RegisterType<ITapeView, TapeToolView>();
            resolver.RegisterType<IMachineSettingsView, MachineSettingsToolView>();
            resolver.RegisterType<IMemoryMapView, MemoryMapToolView>();
            resolver.RegisterType<IAboutView, AboutToolView>();
            resolver.RegisterType<IKeyboardView, KeyboardHelpToolView>();
            resolver.RegisterType<IFddDebugView, FddDebugToolView>();

            resolver.RegisterType<IMainView, AvaloniaMainView>();
            resolver.RegisterType<ILauncher, Launcher>(true);
            resolver.RegisterType<IMainViewModel, MainViewModel>();
            resolver.RegisterType<IPsgChip, PsgChip>();

            Locator.Init(resolver);
            var launcher = Locator.Resolve<ILauncher>();
            launcher.Run(options.Args);
            Locator.Shutdown();
        }
    }
}
