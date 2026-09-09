using System.Collections.Generic;
using System.Linq;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Presentation;
using ZXMAK2.Host.Presentation.Interfaces;
using ZXMAK2.Mvvm;

namespace Kozynax.UI
{
    public static class MainMenuFactory
    {
        public static MainMenu Create(
            MainViewModel viewModel,
            IEnumerable<ICommand> toolCommands,
            object commandParameter)
        {
            var root = BuildRoot(viewModel, toolCommands);
            return new MainMenu(root, commandParameter);
        }

        public static MenuNode BuildRoot(MainViewModel vm, IEnumerable<ICommand> toolCommands)
        {
            return new MenuNode
            {
                Caption = "Menu",
                Children = new List<MenuNode>
                {
                    Branch("File",
                        Cmd(vm.CommandFileOpen),
                        Cmd(vm.CommandFileSave),
                        Cmd(vm.CommandFileExit)),
                    Branch("View",
                        Cmd(vm.CommandViewFullScreen),
                        Branch("Size",
                            Cmd(vm.CommandViewScaleRatio, 1, "100%"),
                            Cmd(vm.CommandViewScaleRatio, 2, "200%"),
                            Cmd(vm.CommandViewScaleRatio, 3, "300%"),
                            Cmd(vm.CommandViewScaleRatio, 4, "400%")),
                        Branch("Scale Mode",
                            Cmd(vm.CommandViewScaleMode, ScaleMode.Stretch, "Stretch",
                                () => vm.RenderScaleMode == ScaleMode.Stretch),
                            Cmd(vm.CommandViewScaleMode, ScaleMode.KeepProportion, "Keep Proportion",
                                () => vm.RenderScaleMode == ScaleMode.KeepProportion),
                            Cmd(vm.CommandViewScaleMode, ScaleMode.FixedPixelSize, "Fixed Pixel Size",
                                () => vm.RenderScaleMode == ScaleMode.FixedPixelSize),
                            Cmd(vm.CommandViewScaleMode, ScaleMode.SquarePixelSize, "Square Pixel Size",
                                () => vm.RenderScaleMode == ScaleMode.SquarePixelSize)),
                        Branch("Video Filter",
                            Cmd(vm.CommandViewVideoFilter, VideoFilter.None, "None",
                                () => vm.RenderVideoFilter == VideoFilter.None),
                            Cmd(vm.CommandViewVideoFilter, VideoFilter.NoFlick, "No Flick",
                                () => vm.RenderVideoFilter == VideoFilter.NoFlick)),
                        Branch("Frame Sync Source",
                            Cmd(vm.CommandViewSyncSource, SyncSource.Time, "Time",
                                () => vm.SyncSource == SyncSource.Time),
                            Cmd(vm.CommandViewSyncSource, SyncSource.Sound, "Sound",
                                () => vm.SyncSource == SyncSource.Sound),
                            Cmd(vm.CommandViewSyncSource, SyncSource.Video, "Video",
                                () => vm.SyncSource == SyncSource.Video)),
                        Cmd(vm.CommandViewSmooth, null, null, () => vm.CommandViewSmooth.Checked),
                        Cmd(vm.CommandViewMimicTv, null, null, () => vm.CommandViewMimicTv.Checked),
                        Cmd(vm.CommandViewDisplayIcon, null, null, () => vm.CommandViewDisplayIcon.Checked),
                        Cmd(vm.CommandViewDebugInfo, null, null, () => vm.CommandViewDebugInfo.Checked)),
                    Branch("VM",
                        Cmd(vm.CommandVmPause),
                        Cmd(vm.CommandVmMaxSpeed, null, null, () => vm.CommandVmMaxSpeed.Checked),
                        Cmd(vm.CommandVmWarmReset),
                        Cmd(vm.CommandVmNmi),
                        Cmd(vm.CommandVmSettings)),
                    Branch("Tools", BuildToolNodes(toolCommands).ToArray()),
                    Branch("Help",
                        Cmd(vm.CommandHelpViewHelp),
                        Cmd(vm.CommandHelpKeyboardHelp),
                        Cmd(vm.CommandHelpAbout)),
                },
            };
        }

        private static IEnumerable<MenuNode> BuildToolNodes(IEnumerable<ICommand> toolCommands)
        {
            if (toolCommands == null)
                yield break;
            foreach (var command in toolCommands.Where(c => c != null).OrderBy(c => c.Text))
                yield return Cmd(command);
        }

        private static MenuNode Branch(string caption, params MenuNode[] children)
            => new MenuNode
            {
                Caption = caption,
                Children = children?.Where(c => c != null).ToList() ?? new List<MenuNode>(),
            };

        private static MenuNode Cmd(
            ICommand command,
            object parameter = null,
            string caption = null,
            System.Func<bool> isChecked = null)
        {
            if (command == null)
                return null;
            return new MenuNode
            {
                Caption = caption ?? command.Text ?? "Command",
                Command = command,
                Parameter = parameter,
                IsChecked = isChecked,
            };
        }
    }
}
