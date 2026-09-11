using System.Drawing;
using System.IO;
using System.Reflection;

namespace ZXMAK2.Resources
{
    public class ResourceImages
    {
        public static Image EmuFileOpen_32x32 => LoadImage("ZXMAK2.Resources.Icons.EmuFileOpen_32x32.png");
        public static Image EmuFileSave_32x32 => LoadImage("ZXMAK2.Resources.Icons.EmuFileSave_32x32.png");
        public static Image EmuResume_32x32 => LoadImage("ZXMAK2.Resources.Icons.EmuResume_32x32.png");
        public static Image EmuMaxSpeed_32x32 => LoadImage("ZXMAK2.Resources.Icons.EmuMaxSpeed_32x32.png");
        public static Image EmuWarmReset_32x32 => LoadImage("ZXMAK2.Resources.Icons.EmuWarmReset_32x32.png");
        public static Image EmuColdReset_32x32 => LoadImage("ZXMAK2.Resources.Icons.EmuColdReset_32x32.png");
        public static Image EmuFullScreen_32x32 => LoadImage("ZXMAK2.Resources.Icons.EmuFullScreen_32x32.png");
        public static Image EmuQuickLoad_32x32 => LoadImage("ZXMAK2.Resources.Icons.EmuQuickLoad_32x32.png");
        public static Image EmuSettings_32x32 => LoadImage("ZXMAK2.Resources.Icons.EmuSettings_32x32.png");
        public static Image EmuWindowed_32x32 => LoadImage("ZXMAK2.Resources.Icons.EmuWindowed_32x32.png");
        public static Image EmuPause_32x32 => LoadImage("ZXMAK2.Resources.Icons.EmuPause_32x32.png");
        public static Image DebuggerClose => LoadImage("ZXMAK2.Resources.Icons.DebuggerClose.png");
        public static Image HardwareTapeAutoplay => LoadImage("ZXMAK2.Resources.Icons.HardwareTapeAutoplay.png");
        public static Image DebuggerBreak => LoadImage("ZXMAK2.Resources.Icons.DebuggerBreak.png");
        public static Image DebuggerContinue => LoadImage("ZXMAK2.Resources.Icons.DebuggerContinue.png");
        public static Image DebuggerShowBreakpoints => LoadImage("ZXMAK2.Resources.Icons.DebuggerShowBreakpoints.png");
        public static Image DebuggerShowNext => LoadImage("ZXMAK2.Resources.Icons.DebuggerShowNext.png");
        public static Image DebuggerStepInto => LoadImage("ZXMAK2.Resources.Icons.DebuggerStepInto.png");
        public static Image DebuggerStepOut => LoadImage("ZXMAK2.Resources.Icons.DebuggerStepOut.png");
        public static Image DebuggerStepOver => LoadImage("ZXMAK2.Resources.Icons.DebuggerStepOver.png");
        public static Stream OsdFddRd => OpenStream("ZXMAK2.Resources.Icons.OsdFddRd.png");
        public static Stream OsdFddWr => OpenStream("ZXMAK2.Resources.Icons.OsdFddWr.png");
        public static Stream OsdHddRd => OpenStream("ZXMAK2.Resources.Icons.OsdHddRd.png");
        public static Image HardwareTapeNext => LoadImage("ZXMAK2.Resources.Icons.HardwareTapeNext.png");
        public static Stream OsdPause => OpenStream("ZXMAK2.Resources.Icons.OsdPause.png");
        public static Image HardwareTapePlay => LoadImage("ZXMAK2.Resources.Icons.HardwareTapePlay.png");
        public static Image HardwareTapePrev => LoadImage("ZXMAK2.Resources.Icons.HardwareTapePrev.png");
        public static Image HardwareTapeRecord => LoadImage("ZXMAK2.Resources.Icons.HardwareTapeRecord.png");
        public static Image HardwareTapeRewind => LoadImage("ZXMAK2.Resources.Icons.HardwareTapeRewind.png");
        public static Image HardwareTapeTraps => LoadImage("ZXMAK2.Resources.Icons.HardwareTapeTraps.png");
        public static Image HardwareTapePause => LoadImage("ZXMAK2.Resources.Icons.HardwareTapePause.png");
        public static Image Stop_real_16x16 => LoadImage("ZXMAK2.Resources.Icons.Stop_real_16x16.png");
        public static Stream OsdTapeRd => OpenStream("ZXMAK2.Resources.Icons.OsdTapeRd.png");

        /// <summary>PNG stream for hosts that cannot use System.Drawing (e.g. SDL on Linux).</summary>
        public static Stream EmuFileOpenPng => OpenStream("ZXMAK2.Resources.Icons.EmuFileOpen_32x32.png");
        public static Stream EmuFileSavePng => OpenStream("ZXMAK2.Resources.Icons.EmuFileSave_32x32.png");
        public static Stream EmuPausePng => OpenStream("ZXMAK2.Resources.Icons.EmuPause_32x32.png");
        public static Stream EmuResumePng => OpenStream("ZXMAK2.Resources.Icons.EmuResume_32x32.png");
        public static Stream EmuMaxSpeedPng => OpenStream("ZXMAK2.Resources.Icons.EmuMaxSpeed_32x32.png");
        public static Stream EmuWarmResetPng => OpenStream("ZXMAK2.Resources.Icons.EmuWarmReset_32x32.png");
        public static Stream EmuColdResetPng => OpenStream("ZXMAK2.Resources.Icons.EmuColdReset_32x32.png");
        public static Stream EmuFullScreenPng => OpenStream("ZXMAK2.Resources.Icons.EmuFullScreen_32x32.png");
        public static Stream EmuWindowedPng => OpenStream("ZXMAK2.Resources.Icons.EmuWindowed_32x32.png");
        public static Stream EmuQuickLoadPng => OpenStream("ZXMAK2.Resources.Icons.EmuQuickLoad_32x32.png");
        public static Stream EmuSettingsPng => OpenStream("ZXMAK2.Resources.Icons.EmuSettings_32x32.png");
        public static Image KeyboardHelp => LoadImage("ZXMAK2.Resources.Icons.KeyboardHelp.png");
        public static Icon IconDebugger => LoadIcon("ZXMAK2.Resources.Icons.IconDebugger.ico");
        public static Icon IconApp => new Icon(OpenStream("ZXMAK2.Resources.Icons.IconApp.ico"), 64, 64);
        public static Stream IconAppPng => OpenStream("ZXMAK2.Resources.Icons.IconApp.png");
        public static Icon ImageZxLogo => LoadIcon("ZXMAK2.Resources.Pictures.ZxLogo.png");
        public static Image ImageKeyboardHelp => LoadImage("ZXMAK2.Resources.Pictures.KeyboardHelp.png");
        public static Stream ImageKeyboardHelpPng => OpenStream("ZXMAK2.Resources.Pictures.KeyboardHelp.png");
        public static Image Wizard => LoadImage("ZXMAK2.Resources.Icons.Wizard.png");
        public static Image RAM => LoadImage("ZXMAK2.Resources.DeviceIcons.RAM.png");
        public static Image PCB => LoadImage("ZXMAK2.Resources.DeviceIcons.PCB.png");
        public static Image ULA => LoadImage("ZXMAK2.Resources.DeviceIcons.ULA.png");
        public static Image FDD => LoadImage("ZXMAK2.Resources.DeviceIcons.FDD.png");
        public static Image BEEPER => LoadImage("ZXMAK2.Resources.DeviceIcons.BEEPER.png");
        public static Image AY8910 => LoadImage("ZXMAK2.Resources.DeviceIcons.AY8910.png");
        public static Image TAPE => LoadImage("ZXMAK2.Resources.DeviceIcons.TAPE.png");
        public static Image KBD => LoadImage("ZXMAK2.Resources.DeviceIcons.KBD.png");
        public static Image MOUS => LoadImage("ZXMAK2.Resources.DeviceIcons.MOUS.png");
        public static Image DEBUG => LoadImage("ZXMAK2.Resources.DeviceIcons.DEBUG.png");
        public static Image DISPLAY => LoadImage("ZXMAK2.Resources.DeviceIcons.DISPLAY.png");

        private static readonly Assembly Assembly = typeof(ResourceImages).GetTypeInfo().Assembly;

        private static Stream OpenStream(string name)
        {
            var resource = Assembly.GetManifestResourceStream(name);
            if (resource == null)
                throw new FileNotFoundException("Resource not found: " + name, name);
            return resource;
        }

        private static Image LoadImage(string name)
            => Image.FromStream(OpenStream(name));

        private static Icon LoadIcon(string name)
            => new Icon(OpenStream(name));
    }
}
