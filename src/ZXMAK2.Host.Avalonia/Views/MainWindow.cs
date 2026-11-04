using System;
using Avalonia.Controls;
using Kozynax.UI.Helpers;
using ZXMAK2.Resources;

namespace ZXMAK2.Host.AvaloniaBackend.Views
{
    /// <summary>
    /// Main shell: menu bar on top, emulator <see cref="FrameSurface"/> filling the rest.
    /// Behaviour lives in <see cref="AvaloniaMainView"/>; this is layout only.
    /// </summary>
    public sealed class MainWindow : Window
    {
        public MainWindow()
        {
            Title = MainWindowTitle.ProductName;
            MinWidth = 160;
            MinHeight = 120;

            Menu = new Menu();
            DockPanel.SetDock(Menu, Dock.Top);

            Surface = new FrameSurface();

            Content = new DockPanel
            {
                LastChildFill = true,
                Children = { Menu, Surface },
            };

            TryApplyIcon();
        }

        public Menu Menu { get; }
        public FrameSurface Surface { get; }

        private void TryApplyIcon()
        {
            try
            {
                using (var stream = ResourceImages.IconAppPng)
                {
                    if (stream != null)
                        Icon = new WindowIcon(stream);
                }
            }
            catch (Exception ex)
            {
                Logger.Warn("Window icon not applied: {0}", ex.Message);
            }
        }
    }
}
