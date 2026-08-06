using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ZXMAK2.Host.Presentation;
using ZXMAK2.Mvvm;
using ZXMAK2.Resources;

namespace Kozynax.UI
{
    public sealed class MenuToolbarItem
    {
        public bool IsSeparator { get; set; }
        public ICommand Command { get; set; }
        public object Parameter { get; set; }
        public Func<bool> IsChecked { get; set; }
        public Func<Stream> Image { get; set; }
        public Func<string> ImageKey { get; set; }
        public string Tip { get; set; }
    }

    public static class MenuToolbarFactory
    {
        public static List<MenuToolbarItem> Create(MainViewModel vm)
        {
            if (vm == null)
                throw new ArgumentNullException(nameof(vm));

            return new MenuToolbarItem[]
            {
                Button(vm.CommandFileOpen, () => ResourceImages.EmuFileOpenPng, () => "open", "Open"),
                Button(vm.CommandFileSave, () => ResourceImages.EmuFileSavePng, () => "save", "Save"),
                Separator(),
                Button(
                    vm.CommandVmPause,
                    () => vm.IsRunning ? ResourceImages.EmuPausePng : ResourceImages.EmuResumePng,
                    () => vm.IsRunning ? "pause" : "resume",
                    "Pause / Resume"),
                Button(
                    vm.CommandVmMaxSpeed,
                    () => ResourceImages.EmuMaxSpeedPng,
                    () => "maxspeed",
                    "Maximum Speed",
                    () => vm.CommandVmMaxSpeed.Checked),
                Button(vm.CommandVmWarmReset, () => ResourceImages.EmuWarmResetPng, () => "warm", "Warm Reset"),
                Separator(),
                Button(
                    vm.CommandViewFullScreen,
                    () => vm.IsFullScreen ? ResourceImages.EmuWindowedPng : ResourceImages.EmuFullScreenPng,
                    () => vm.IsFullScreen ? "windowed" : "fullscreen",
                    "Full Screen"),
                Button(vm.CommandQuickLoad, () => ResourceImages.EmuQuickLoadPng, () => "quick", "Quick Boot"),
                Button(vm.CommandVmSettings, () => ResourceImages.EmuSettingsPng, () => "settings", "Settings"),
            }.Where(i => i != null).ToList();
        }

        private static MenuToolbarItem Separator()
            => new MenuToolbarItem { IsSeparator = true };

        private static MenuToolbarItem Button(
            ICommand command,
            Func<Stream> image,
            Func<string> imageKey,
            string tip,
            Func<bool> isChecked = null)
        {
            if (command == null || image == null)
                return null;
            return new MenuToolbarItem
            {
                Command = command,
                Image = image,
                ImageKey = imageKey,
                Tip = tip,
                IsChecked = isChecked,
            };
        }
    }
}
