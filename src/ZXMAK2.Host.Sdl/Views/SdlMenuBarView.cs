using System;
using System.Collections.Generic;
using Kozynax.UI;
using ZXMAK2.Host.Presentation;
using ZXMAK2.Host.Terminal;
using ZXMAK2.Mvvm;

namespace ZXMAK2.Host.SdlBackend.Views
{
    /// <summary>
    /// SDL F9 host for menu chrome (toolbar strip + horizontal menu bar).
    /// After a command (or dismiss), control returns to emulation — the bar is not reopened.
    /// </summary>
    public static class SdlMenuBarView
    {
        public static void Show(
            ITerminal terminal,
            MainViewModel viewModel,
            IEnumerable<ICommand> toolCommands,
            object commandParameter,
            Func<bool> isHostQuitting = null,
            Action underlay = null,
            IMenuImagePainter imagePainter = null)
        {
            if (terminal == null || !terminal.IsAvailable || viewModel == null)
                return;
            if (isHostQuitting != null && isHostQuitting())
                return;

            var root = MainMenuFactory.BuildRoot(viewModel, toolCommands);
            var toolbar = MenuToolbarFactory.Create(viewModel);
            terminal.PrepareForUiInput();
            try
            {
                var screen = new MenuChromeScreen(terminal, commandParameter)
                {
                    Underlay = underlay,
                    Toolbar = toolbar,
                    ImagePainter = imagePainter,
                };
                screen.Run(root);
            }
            finally
            {
                terminal.EndUiInput();
            }
        }
    }
}
