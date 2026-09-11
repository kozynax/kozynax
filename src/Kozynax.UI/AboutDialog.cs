using System;
using System.Diagnostics;
using System.Reflection;
using Kozui.Abstract;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Host.WinForms.Lib.Layout;

namespace Kozynax.UI
{
    /// <summary>
    /// Kozui About dialog (title, version, project URL, license text).
    /// </summary>
    [KozuiDialog(CaptureBackdrop = true)]
    public sealed class AboutDialog : ViewDescription<AboutDialog>
    {
        public const string ProjectUrl = "https://github.com/kozynax/kozynax";

        public const string LicenseText = @"Copyright 2026 Alexander Tsidaev (Eltaron/INK9)

Original (C) for the emulation engine and Windows
Forms UI belongs to ZXMAK2 contributors:
  - Alexander Makeev (ZXMAK, project founder)
  - SMT (author of UnrealSpeccy emulator)
  - Hard/WCG (Дмитрий Михальченков)
  - ZEK (Демьяненко Дмитрий)
  - Eltaron (Alexander Tsidaev).

Kozynax is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

Kozynax is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Kozynax.  If not, see <http://www.gnu.org/licenses/>.

***

Portions of this software are copyright (c) Amstrad Consumer Electronics plc.
Amstrad have kindly given their permission for the redistribution of their
copyrighted material but retain that copyright.";

        public event EventHandler CloseRequested;

        public DlgResult DialogResult { get; private set; } = DlgResult.Cancel;

        public Panel Root { get; }
        public Label TitleLabel { get; }
        public Label ProductLabel { get; }
        public Label VersionLabel { get; }
        public Button UrlButton { get; }
        public ListView<string> LicenseList { get; }
        public Button OkButton { get; }

        public AboutDialog()
        {
            TitleLabel = new Label
            {
                Text = "About Kozynax",
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            ProductLabel = new Label
            {
                Text = "Kozynax",
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 1, 0, 0),
            };
            VersionLabel = new Label
            {
                Text = "Version " + GetProductVersion(),
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            UrlButton = new Button { Text = ProjectUrl };
            UrlButton.Clicked += (_, __) => OpenProjectUrl();

            LicenseList = new ListView<string>
            {
                ItemTextSelector = line => line ?? string.Empty,
                Dock = Dock.Fill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                MinHeight = 10,
                MinWidth = 48,
                Margin = new Thickness(0, 1, 0, 1),
            };
            LicenseList.Reset(LicenseText.Replace("\r\n", "\n").Split('\n'));

            OkButton = new Button { Text = "OK" };
            OkButton.Clicked += (_, __) => Accept();

            Root = BuildTree();
        }

        public void Accept()
        {
            DialogResult = DlgResult.OK;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private Panel BuildTree()
        {
            var header = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            header.Add(TitleLabel);
            header.Add(ProductLabel);
            header.Add(VersionLabel);

            var urlRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 1,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 1, 0, 0),
            };
            urlRow.Add(UrlButton);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            buttons.Add(OkButton);

            var content = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 0,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(2),
                MinWidth = 52,
                MinHeight = 16,
            };
            content.Add(header);
            content.Add(urlRow);
            content.Add(LicenseList);
            content.Add(buttons);

            var frame = new Placeholder
            {
                Content = content,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2),
                MinWidth = 54,
                MinHeight = 18,
            };

            var root = new Panel();
            root.Add(frame);
            return root;
        }

        public static string GetProductVersion()
        {
            var asm = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (!string.IsNullOrWhiteSpace(info?.InformationalVersion))
            {
                var value = info.InformationalVersion;
                var plus = value.IndexOf('+');
                return plus >= 0 ? value.Substring(0, plus) : value;
            }

            var version = asm.GetName().Version;
            if (version != null)
                return $"{version.Major}.{version.Minor}" + (version.Build > 0 ? $".{version.Build}" : "");
            
            return "0.0";
        }

        public static void OpenProjectUrl()
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = ProjectUrl,
                    UseShellExecute = true,
                });
            }
            catch (Exception ex)
            {
                global::ZXMAK2.Logger.Error(ex);
            }
        }
    }
}
