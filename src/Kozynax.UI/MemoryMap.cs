using System;
using System.Collections.Generic;
using System.Reflection;
using Kozui.Abstract;
using ZXMAK2.Dependency;
using ZXMAK2.Engine.Attributes;
using ZXMAK2.Hardware;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Host.WinForms.Lib.Layout;
using Button = ZXMAK2.Host.WinForms.Lib.Button;
using Timer = ZXMAK2.Host.WinForms.Lib.Timer;

namespace Kozynax.UI
{
    /// <summary>
    /// Kozui tree for the memory map tool window (CMR, windows, hardware props).
    /// </summary>
    [KozuiDialog(CaptureBackdrop = false)]
    public sealed class MemoryMap : ViewDescription<MemoryMap>
    {
        private const string Unknown = "???";

        private readonly MemoryBase _memory;
        private readonly List<HardwareProp> _props = new List<HardwareProp>();

        public event EventHandler CloseRequested;

        public DlgResult DialogResult { get; private set; } = DlgResult.Cancel;

        public Panel Root { get; }
        public Label Cmr0Value { get; }
        public Label Cmr1Value { get; }
        public Label Window0000 { get; }
        public Label Window4000 { get; }
        public Label Window8000 { get; }
        public Label WindowC000 { get; }
        public CheckBox Dosen { get; }
        public CheckBox Sysen { get; }
        public ListView<HardwareProp> Properties { get; }
        public Timer UpdateTimer { get; }
        public Button CloseButton { get; }
        public Button Cmr0Edit { get; }
        public Button Cmr1Edit { get; }

        public MemoryMap(MemoryBase memory)
        {
            _memory = memory ?? throw new ArgumentNullException(nameof(memory));

            Cmr0Value = new Label { Text = "#00" };
            Cmr1Value = new Label { Text = "#00" };
            Window0000 = new Label { Text = Unknown };
            Window4000 = new Label { Text = Unknown };
            Window8000 = new Label { Text = Unknown };
            WindowC000 = new Label { Text = Unknown };
            // Display-only: WinForms uses AutoCheck=false; Terminal shows state only.
            Dosen = new CheckBox { Text = "DOSEN" };
            Sysen = new CheckBox { Text = "SYSEN" };

            Properties = new ListView<HardwareProp>
            {
                ItemTextSelector = FormatProp,
                ActivateOnClick = true,
                Dock = Dock.Fill,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                MinHeight = 4,
            };
            Properties.ItemActivated += Properties_ItemActivated;

            UpdateTimer = new Timer { IntervalMs = 250, Enabled = true };
            UpdateTimer.OnTick += (_, __) => Refresh();

            CloseButton = new Button { Text = "Close" };
            CloseButton.Clicked += (_, __) => Accept();

            Cmr0Edit = new Button { Text = "Edit" };
            Cmr1Edit = new Button { Text = "Edit" };
            Cmr0Edit.Clicked += (_, __) => EditCmr0();
            Cmr1Edit.Clicked += (_, __) => EditCmr1();

            Root = BuildTree();
            CollectHardwareProps();
            Refresh();
        }

        public void Accept()
        {
            DialogResult = DlgResult.OK;
            UpdateTimer.Enabled = false;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        public void Refresh()
        {
            if (_memory == null)
            {
                Cmr0Value.Text = Unknown;
                Cmr1Value.Text = Unknown;
                Window0000.Text = Unknown;
                Window4000.Text = Unknown;
                Window8000.Text = Unknown;
                WindowC000.Text = Unknown;
                Dosen.Checked = false;
                Sysen.Checked = false;
                return;
            }

            Cmr0Value.Text = string.Format("#{0:X2}", _memory.CMR0);
            Cmr1Value.Text = string.Format("#{0:X2}", _memory.CMR1);
            Window0000.Text = FindPageName(_memory.Window0000);
            Window4000.Text = FindPageName(_memory.Window4000);
            Window8000.Text = FindPageName(_memory.Window8000);
            WindowC000.Text = FindPageName(_memory.WindowC000);
            Dosen.Checked = _memory.DOSEN;
            Sysen.Checked = _memory.SYSEN;

            if (_props.Count > 0)
            {
                var selected = Properties.SelectedIndex;
                Properties.Reset(_props);
                if (selected >= 0 && selected < _props.Count)
                    Properties.SelectedIndex = selected;
            }
        }

        public void EditCmr0() => EditCmr(isCmr0: true);

        public void EditCmr1() => EditCmr(isCmr0: false);

        public void Close()
        {
            UpdateTimer.Enabled = false;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private Panel BuildTree()
        {
            var title = new Label
            {
                Text = "Memory Map",
                Dock = Dock.Top,
                Margin = new Thickness(0, 0, 0, 1),
            };

            var cmr0Row = Row("CMR0:", Cmr0Value, Cmr0Edit);
            var cmr1Row = Row("CMR1:", Cmr1Value, Cmr1Edit);

            var flags = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                Dock = Dock.Top,
                Margin = new Thickness(0, 0, 0, 1),
            };
            flags.Add(Dosen);
            flags.Add(Sysen);

            var windows = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 0,
                Dock = Dock.Top,
                Margin = new Thickness(0, 0, 0, 1),
            };
            windows.Add(WindowRow("#0000-#3FFF:", Window0000));
            windows.Add(WindowRow("#4000-#7FFF:", Window4000));
            windows.Add(WindowRow("#8000-#BFFF:", Window8000));
            windows.Add(WindowRow("#C000-#FFFF:", WindowC000));

            var propsTitle = new Label
            {
                Text = "Hardware (Enter edit/toggle)",
                Dock = Dock.Top,
                Margin = new Thickness(0, 0, 0, 1),
            };

            var footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                Dock = Dock.Bottom,
                Margin = new Thickness(0, 1, 0, 0),
            };
            footer.Add(CloseButton);
            footer.Add(new Label { Text = "Esc close" });

            var root = new DockPanel { Margin = new Thickness(1) };
            root.Add(title);
            root.Add(cmr0Row);
            root.Add(cmr1Row);
            root.Add(flags);
            root.Add(windows);
            root.Add(propsTitle);
            root.Add(footer);
            root.Add(Properties);
            return root;
        }

        private static StackPanel Row(string caption, Label value, params Button[] buttons)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 1,
                Dock = Dock.Top,
                Margin = new Thickness(0, 0, 0, 0),
            };
            row.Add(new Label { Text = caption, MinWidth = 6 });
            row.Add(value);
            foreach (var button in buttons)
                row.Add(button);
            return row;
        }

        private static StackPanel WindowRow(string caption, Label value)
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 1,
            };
            row.Add(new Label { Text = caption, MinWidth = 14 });
            row.Add(value);
            return row;
        }

        private void EditCmr(bool isCmr0)
        {
            var service = Locator.TryResolve<IUserQuery>();
            if (service == null)
                return;

            var value = isCmr0 ? (int)_memory.CMR0 : (int)_memory.CMR1;
            var name = isCmr0 ? "CMR0" : "CMR1";
            if (!service.QueryValue(
                string.Format("Change {0}", name),
                string.Format("New {0} value", name),
                "#{0:X2}",
                ref value,
                0,
                255))
            {
                return;
            }

            if (isCmr0)
                _memory.CMR0 = (byte)value;
            else
                _memory.CMR1 = (byte)value;
            Refresh();
        }

        private string FindPageName(byte[] wndRead)
        {
            if (wndRead == null)
                return Unknown;

            for (var i = 0; i < _memory.RomPages.Length; i++)
            {
                if (_memory.RomPages[i] != wndRead)
                    continue;
                var romName = _memory.GetRomName(i);
                if (string.IsNullOrEmpty(romName))
                    return string.Format("ROM #{0:X2}", i);
                return string.Format("ROM #{0:X2} ({1})", i, romName);
            }

            for (var i = 0; i < _memory.RamPages.Length; i++)
            {
                if (_memory.RamPages[i] == wndRead)
                    return string.Format("RAM #{0:X2}", i);
            }

            return Unknown;
        }

        private void CollectHardwareProps()
        {
            _props.Clear();
            foreach (var pi in _memory.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                string displayName = null;
                var readOnly = false;
                foreach (Attribute at in pi.GetCustomAttributes(true))
                {
                    if (at is HardwareValueAttribute hv)
                        displayName = hv.Name;
                    else if (at is HardwareSwitchAttribute hs)
                        displayName = hs.Name;
                    else if (at is HardwareReadOnlyAttribute ro)
                        readOnly = ro.IsReadOnly;
                }
                if (displayName == null)
                    continue;
                _props.Add(new HardwareProp(displayName, pi, readOnly) { Owner = _memory });
            }
            Properties.Reset(_props);
            Properties.SelectedIndex = _props.Count > 0 ? 0 : -1;
        }

        private static string FormatProp(HardwareProp prop)
        {
            if (prop == null || prop.Owner == null)
                return string.Empty;
            object raw;
            try
            {
                raw = prop.Info.GetValue(prop.Owner, null);
            }
            catch
            {
                return prop.Name + ": ?";
            }

            string value;
            if (raw is bool b)
                value = b ? "on" : "off";
            else if (raw is byte by)
                value = string.Format("#{0:X2}", by);
            else if (raw is int i)
                value = string.Format("#{0:X}", i);
            else
                value = raw?.ToString() ?? "?";

            return (prop.IsReadOnly ? "  " : "> ") + prop.Name + ": " + value;
        }

        private void Properties_ItemActivated(object sender, EventArgs e)
        {
            var index = Properties.SelectedIndex;
            if (index < 0 || index >= _props.Count)
                return;

            var prop = _props[index];
            prop.Owner = _memory;
            if (prop.IsReadOnly)
                return;

            object raw;
            try
            {
                raw = prop.Info.GetValue(_memory, null);
            }
            catch
            {
                return;
            }

            if (raw is bool b)
            {
                prop.Info.SetValue(_memory, !b, null);
                Refresh();
                return;
            }

            if (raw is byte || raw is int)
            {
                var service = Locator.TryResolve<IUserQuery>();
                if (service == null)
                    return;
                var value = Convert.ToInt32(raw);
                var max = raw is byte ? 255 : int.MaxValue;
                if (!service.QueryValue(
                    "Change " + prop.Name,
                    "New " + prop.Name + " value",
                    raw is byte ? "#{0:X2}" : "#{0:X}",
                    ref value,
                    0,
                    max))
                {
                    return;
                }

                if (raw is byte)
                    prop.Info.SetValue(_memory, (byte)value, null);
                else
                    prop.Info.SetValue(_memory, value, null);
                Refresh();
            }
        }

        public sealed class HardwareProp
        {
            public HardwareProp(string name, PropertyInfo info, bool isReadOnly)
            {
                Name = name;
                Info = info;
                IsReadOnly = isReadOnly;
            }

            public string Name { get; }
            public PropertyInfo Info { get; }
            public bool IsReadOnly { get; }
            public MemoryBase Owner { get; set; }
        }
    }
}
