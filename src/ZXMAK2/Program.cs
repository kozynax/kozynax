using System;
using Kozui.Interfaces;
using Kozynax.UI;
using ZXMAK2.Dependency;
using ZXMAK2.Hardware.Circuits.Sound;
using ZXMAK2.Hardware.Sprinter;
using ZXMAK2.Hardware.WinForms;
using ZXMAK2.Hardware.WinForms.General;
using ZXMAK2.Hardware.WinForms.Sprinter;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.Presentation;
using ZXMAK2.Host.Presentation.Interfaces;
using ZXMAK2.Host.Services;
using ZXMAK2.Host.WinForms.Mdx;
using ZXMAK2.Host.WinForms.Services;
using ZXMAK2.Host.WinForms.Views;


namespace ZXMAK2
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException +=
                    (s, e) => Logger.Fatal(e.ExceptionObject as Exception, "AppDomain.UnhandledException");
                //AppDomain.CurrentDomain.FirstChanceException += 
                //    (s, e) => Logger.Error(e.Exception, "AppDomain.FirstChanceException");
                RunSafe(args);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private static void RunSafe(string[] args)
        {
            var resolver = new ResolverUnity();

            resolver.RegisterInstance<IResolver>(resolver);
            resolver.RegisterType<ISettingService, SettingService>(true);
            resolver.RegisterType<IUserMessage, UserMessage>();
            resolver.RegisterType<IUserQuery, UserQuery>();
            resolver.RegisterType<IUserHelp, UserHelp>();
            resolver.RegisterType<IOpenFileDialog, OpenFileDialog>();
            resolver.RegisterType<ISaveFileDialog, SaveFileDialog>();

            resolver.RegisterType<IMainView, MainView>();
            resolver.RegisterType<IAboutView, FormAbout>();
            resolver.RegisterType<IKeyboardView, FormKeyboardHelp>();

            resolver.RegisterType<IMachineSettingsView, FormMachineSettings>();
            resolver.RegisterType<IMemoryMapView, FormMemoryMap>();
            resolver.RegisterType<ITapeView, TapeForm>();
            resolver.RegisterType<IFddDebugView, dbgWD1793>();
            resolver.RegisterType<IDebuggerGeneralView, FormCpu>();
            resolver.RegisterType<IDebuggerSprinterView, DebugForm>();

            resolver.RegisterType<ILauncher, Launcher>(true);
            resolver.RegisterType<IMainViewModel, MainViewModel>();
            resolver.RegisterType<IPsgChip, PsgChip>();

            resolver.RegisterType<IViewImplementation<MachineSettings>, FormMachineSettings>();
            resolver.RegisterType<IViewImplementation<AddDeviceDialog>, FormAddDeviceWizard>();
            resolver.RegisterType<IViewImplementation<DebuggerDialog>, FormCpu>();
            resolver.RegisterType<IViewImplementation<SprinterDebuggerDialog>, DebugForm>();
            resolver.RegisterType<IViewImplementation<TapeSettings>, TapeForm>();
            
            Locator.Init(resolver);
            var launcher = Locator.Resolve<ILauncher>();
            launcher.Run(args);
            Locator.Shutdown();
        }
    }
}
