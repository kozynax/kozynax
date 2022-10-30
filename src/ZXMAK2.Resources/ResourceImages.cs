using System.Drawing;

namespace ZXMAK2.Resources
{
    public class ResourceImages
    {
        public static Image EmuFileOpen_32x32 => Image.FromFile("Icons/EmuFileOpen_32x32.png");
        public static Image EmuFileSave_32x32 => Image.FromFile("Icons/EmuFileSave_32x32.png");
        public static Image EmuResume_32x32 => Image.FromFile("Icons/EmuResume_32x32.png");
        public static Image EmuMaxSpeed_32x32 => Image.FromFile("Icons/EmuMaxSpeed_32x32.png");
        public static Image EmuWarmReset_32x32 => Image.FromFile("Icons/EmuWarmReset_32x32.png");
        public static Image EmuColdReset_32x32 => Image.FromFile("Icons/EmuColdReset_32x32.png");
        public static Image EmuFullScreen_32x32 => Image.FromFile("Icons/EmuFullScreen_32x32.png");
        public static Image EmuQuickLoad_32x32 => Image.FromFile("Icons/EmuQuickLoad_32x32.png");
        public static Image EmuSettings_32x32 => Image.FromFile("Icons/EmuSettings_32x32.png");
        public static Image EmuWindowed_32x32 => Image.FromFile("Icons/EmuWindowed_32x32.png");
        public static Image EmuPause_32x32 => Image.FromFile("Icons/EmuPause_32x32.png");
        public static Image DebuggerClose => Image.FromFile("Icons/DebuggerClose.png");
        public static Image HardwareTapeAutoplay => Image.FromFile("Icons/HardwareTapeAutoplay.png");
        public static Image DebuggerBreak => Image.FromFile("Icons/DebuggerBreak.png");
        public static Image DebuggerContinue => Image.FromFile("Icons/DebuggerContinue.png");
        public static Image DebuggerShowBreakpoints => Image.FromFile("Icons/DebuggerShowBreakpoints.png");
        public static Image DebuggerShowNext => Image.FromFile("Icons/DebuggerShowNext.png");
        public static Image DebuggerStepInto => Image.FromFile("Icons/DebuggerStepInto.png");
        public static Image DebuggerStepOut => Image.FromFile("Icons/DebuggerStepOut.png");
        public static Image DebuggerStepOver => Image.FromFile("Icons/DebuggerStepOver.png");
        public static Image OsdFddRd => Image.FromFile("Icons/OsdFddRd.png");
        public static Image OsdFddWr => Image.FromFile("Icons/OsdFddWr.png");
        public static Image OsdHddRd => Image.FromFile("Icons/OsdHddRd.png");
        public static Image HardwareTapeNext => Image.FromFile("Icons/HardwareTapeNext.png");
        public static Image OsdPause => Image.FromFile("Icons/OsdPause.png");
        public static Image HardwareTapePlay => Image.FromFile("Icons/HardwareTapePlay.png");
        public static Image HardwareTapePrev => Image.FromFile("Icons/HardwareTapePrev.png");
        public static Image HardwareTapeRecord => Image.FromFile("Icons/HardwareTapeRecord.png");
        public static Image HardwareTapeRewind => Image.FromFile("Icons/HardwareTapeRewind.png");
        public static Image HardwareTapeTraps => Image.FromFile("Icons/HardwareTapeTraps.png");
        public static Image HardwareTapePause => Image.FromFile("Icons/HardwareTapePause.png");
        public static Image Stop_real_16x16 => Image.FromFile("Icons/Stop_real_16x16.png");
        public static Image OsdTapeRd => Image.FromFile("Icons/OsdTapeRd.png");
        public static Image KeyboardHelp => Image.FromFile("Icons/KeyboardHelp.png");
        public static Icon IconDebugger => new Icon("Icons/IconDebugger.ico");
        public static Icon IconApp => new Icon("Icons/IconApp.ico");
        public static Icon ImageZxLogo => new Icon("Icons/ImageZxLogo.png");
        public static Image ImageKeyboardHelp => Image.FromFile("Icons/ImageKeyboardHelp.png");
        public static Image Wizard => Image.FromFile("Icons/Wizard.png");
        public static Image RAM => Image.FromFile("DeviceIcons/RAM.png");
        public static Image PCB => Image.FromFile("DeviceIcons/PCB.png");
        public static Image ULA => Image.FromFile("DeviceIcons/ULA.png");
        public static Image FDD => Image.FromFile("DeviceIcons/FDD.png");
        public static Image BEEPER => Image.FromFile("DeviceIcons/BEEPER.png");
        public static Image AY8910 => Image.FromFile("DeviceIcons/AY8910.png");
        public static Image TAPE => Image.FromFile("DeviceIcons/TAPE.png");
        public static Image KBD => Image.FromFile("DeviceIcons/KBD.png");
        public static Image MOUS => Image.FromFile("DeviceIcons/MOUS.png");
        public static Image DEBUG => Image.FromFile("DeviceIcons/DEBUG.png");
        public static Image DISPLAY => Image.FromFile("DeviceIcons/DISPLAY.png");
        
        // Adlers debugger resources
        public static Icon AdlersAsm => new Icon("Adlers/Asm.ico");
        public static Icon AdlersAsmSettings => new Icon("Adlers/AsmSettings.ico");
        public static Image Adlers_compileToolStrip => Image.FromFile("compileToolStrip.png");
        public static Image Adlers_openFileStripButton => Image.FromFile("openFileStripButton.png");
        public static Image Adlers_saveFileStripButton => Image.FromFile("saveFileStripButton.png");
        public static Image Adlers_settingsToolStrip => Image.FromFile("settingsToolStrip.png");
        public static Image Adlers_toolCodeLibrary => Image.FromFile("toolCodeLibrary.png");
        public static Image Adlers_toolStripButtonReloadFile => Image.FromFile("toolStripButtonReloadFile.png");
        public static Image Adlers_toolStripColors => Image.FromFile("toolStripColors.png");
        public static Image Adlers_toolStripNewSource => Image.FromFile("toolStripNewSource.png");
    }
}