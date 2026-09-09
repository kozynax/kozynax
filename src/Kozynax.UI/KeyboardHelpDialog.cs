using System;
using Kozui.Abstract;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Host.WinForms.Lib.Layout;
using ZXMAK2.Resources;

namespace Kozynax.UI
{
    /// <summary>
    /// Kozui Keyboard Help dialog — Spectrum keyboard legend image + Close.
    /// </summary>
    [KozuiDialog(CaptureBackdrop = true, RequireImagePainter = true)]
    public sealed class KeyboardHelpDialog : ViewDescription<KeyboardHelpDialog>
    {
        public const int ImagePixelWidth = 541;
        public const int ImagePixelHeight = 201;

        public event EventHandler CloseRequested;

        public DlgResult DialogResult { get; private set; } = DlgResult.Cancel;

        public Panel Root { get; }
        public Label TitleLabel { get; }
        public ImageView KeyboardImage { get; }
        public Button CloseButton { get; }

        public KeyboardHelpDialog()
        {
            TitleLabel = new Label
            {
                Text = "Keyboard Help",
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            KeyboardImage = new ImageView
            {
                Image = () => ResourceImages.ImageKeyboardHelpPng,
                ImageKey = "keyboard-help",
                SourceWidth = ImagePixelWidth,
                SourceHeight = ImagePixelHeight,
                MinWidth = 40,
                MinHeight = 12,
            };

            CloseButton = new Button { Text = "Close" };
            CloseButton.Clicked += (_, __) => Accept();

            Root = BuildTree();
        }

        public void Accept()
        {
            DialogResult = DlgResult.OK;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private Panel BuildTree()
        {
            TitleLabel.Dock = Dock.Top;
            TitleLabel.Margin = new Thickness(0, 0, 0, 1);

            CloseButton.Dock = Dock.Bottom;
            CloseButton.HorizontalAlignment = HorizontalAlignment.Center;
            CloseButton.Margin = new Thickness(0, 1, 0, 0);

            KeyboardImage.Dock = Dock.Fill;
            KeyboardImage.Margin = new Thickness(0);

            var content = new DockPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(2),
                MinWidth = 50,
                MinHeight = 18,
            };
            content.Add(TitleLabel);
            content.Add(CloseButton);
            content.Add(KeyboardImage);

            var frame = new Placeholder
            {
                Content = content,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(2),
                MinWidth = 52,
                MinHeight = 20,
            };

            var root = new Panel();
            root.Add(frame);
            return root;
        }
    }
}
