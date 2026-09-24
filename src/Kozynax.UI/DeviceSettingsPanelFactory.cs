using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ZXMAK2;
using ZXMAK2.Dependency;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Entities;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.Interfaces;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Host.WinForms.Lib.Layout;

namespace Kozynax.UI
{
    /// <summary>
    /// Builds Kozui detail panels from <see cref="DeviceSettings{T}"/> instances (no WinForms).
    /// </summary>
    public static class DeviceSettingsPanelFactory
    {
        public sealed class DevicePanel
        {
            public DevicePanel(KozuiControl root, Action apply)
            {
                Root = root;
                Apply = apply ?? (() => { });
            }

            public KozuiControl Root { get; }
            public Action Apply { get; }
        }

        public static DevicePanel Create(BusManager bus, IHostService host, BusDeviceBase device)
        {
            if (device == null)
                return CreateGenericInfo(null);

            var settings = TryCreateSettings(bus, host, device);
            if (settings != null)
            {
                var panel = BuildKnownPanel(settings);
                if (panel != null)
                    return panel;
            }

            return CreateGenericInfo(device);
        }

        private static object TryCreateSettings(BusManager bus, IHostService host, BusDeviceBase device)
        {
            var deviceType = device.GetType();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type settingsType;
                try
                {
                    settingsType = FindDeviceSettingsType(deviceType, asm);
                }
                catch
                {
                    continue;
                }

                if (settingsType == null)
                    continue;

                try
                {
                    var settings = Activator.CreateInstance(settingsType);
                    var init = FindInitMethod(settingsType, deviceType);
                    if (init == null)
                        continue;
                    init.Invoke(settings, new object[] { bus, host, device });
                    return settings;
                }
                catch (Exception ex)
                {
                    global::ZXMAK2.Logger.Error(ex);
                }
            }

            return null;
        }

        private static MethodInfo FindInitMethod(Type settingsType, Type deviceType)
        {
            foreach (var method in settingsType.GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                if (method.Name != "Init")
                    continue;
                var parameters = method.GetParameters();
                if (parameters.Length != 3)
                    continue;
                if (parameters[0].ParameterType != typeof(BusManager))
                    continue;
                if (parameters[1].ParameterType != typeof(IHostService))
                    continue;
                // DeviceSettings<T>.Init(..., T device) — T must accept this device.
                if (!parameters[2].ParameterType.IsAssignableFrom(deviceType))
                    continue;
                return method;
            }
            return null;
        }

        private static Type FindDeviceSettingsType(Type deviceType, Assembly assembly)
        {
            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray();
            }
            catch
            {
                return null;
            }

            var candidates = new List<Type>();
            foreach (var t in types)
            {
                if (t == null || !t.IsClass || t.IsAbstract)
                    continue;
                var arg = GetDeviceSettingsArg(t);
                if (arg == null)
                    continue;
                // Only DeviceSettings<T> where the device is a T (not the reverse —
                // BusDeviceBase.IsAssignableFrom(HayesModem) wrongly matched every device).
                if (arg.IsAssignableFrom(deviceType))
                    candidates.Add(t);
            }

            return candidates
                .OrderByDescending(t => Specificity(t, deviceType))
                .FirstOrDefault();
        }

        /// <summary>
        /// Walks the inheritance chain so <see cref="SingleListViewDeviceSettings{TDevice,TListItem}"/>
        /// subclasses are discovered (not only direct <see cref="DeviceSettings{T}"/>).
        /// </summary>
        private static Type GetDeviceSettingsArg(Type settingsType)
        {
            for (var t = settingsType; t != null && t != typeof(object); t = t.BaseType)
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(DeviceSettings<>))
                    return t.GetGenericArguments()[0];
            }
            return null;
        }

        private static int Specificity(Type settingsType, Type deviceType)
        {
            var arg = GetDeviceSettingsArg(settingsType);
            if (arg == null)
                return 0;
            if (arg == deviceType)
                return 100;
            if (arg.IsAssignableFrom(deviceType))
                return 50;
            return 0;
        }

        private static DevicePanel BuildKnownPanel(object settings)
        {
            if (settings is GenericSoundSettings sound)
                return BuildSound(sound);

            if (settings is BetaDiskSettings beta)
                return BuildBetaDisk(beta);

            if (settings is MemorySettings memory)
                return BuildMemory(memory);

            var type = settings.GetType();
            var baseType = type.BaseType;
            if (baseType != null
                && baseType.IsGenericType
                && baseType.GetGenericTypeDefinition() == typeof(SingleListViewDeviceSettings<,>))
            {
                var title = (Label)type.GetProperty("Title")?.GetValue(settings);
                var listObj = type.GetProperty("List")?.GetValue(settings);
                if (title != null && listObj is KozuiControl listControl)
                {
                    var stack = new StackPanel
                    {
                        Orientation = Orientation.Vertical,
                        Spacing = 1,
                        Margin = new Thickness(0, 0, 0, 0),
                    };
                    title.HorizontalAlignment = HorizontalAlignment.Left;
                    if (listControl is ListView listView)
                    {
                        listView.Dock = Dock.None;
                        listView.HorizontalAlignment = HorizontalAlignment.Stretch;
                        listView.VerticalAlignment = VerticalAlignment.Stretch;
                        listView.MinHeight = 8;
                    }
                    stack.Add(title);
                    stack.Add(listControl);
                    return new DevicePanel(stack, () => type.GetMethod("Apply")?.Invoke(settings, null));
                }
            }

            return null;
        }

        private static DevicePanel BuildMemory(MemorySettings memory)
        {
            memory.TypeList.HorizontalAlignment = HorizontalAlignment.Stretch;
            memory.TypeList.VerticalAlignment = VerticalAlignment.Stretch;
            memory.RomSetList.HorizontalAlignment = HorizontalAlignment.Stretch;
            memory.RomSetList.VerticalAlignment = VerticalAlignment.Stretch;

            var stack = new StackPanel
            {
                Orientation = Orientation.Vertical,
                Spacing = 1,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            stack.Add(memory.TypeTitle);
            stack.Add(memory.TypeList);
            stack.Add(memory.RomSetTitle);
            stack.Add(memory.RomSetList);
            return new DevicePanel(stack, memory.Apply);
        }

        private static DevicePanel BuildSound(GenericSoundSettings sound)
        {
            sound.Volume.MinWidth = 20;
            var label = new Label { Text = "Volume:" };
            var value = new Label { Text = sound.Volume.Value.ToString() };
            sound.Volume.ValueChanged += (_, __) => value.Text = sound.Volume.Value.ToString();

            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 1 };
            row.Add(label);
            row.Add(sound.Volume);
            row.Add(value);

            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 1 };
            stack.Add(new Label { Text = "Sound" });
            stack.Add(row);
            return new DevicePanel(stack, sound.Apply);
        }

        private static DevicePanel BuildBetaDisk(BetaDiskSettings beta)
        {
            beta.NoDelay.Text = "No Delay";
            beta.LogIO.Text = "Log I/O";

            WireDiskBrowse(beta);

            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 1 };
            stack.Add(new Label { Text = "Beta Disk" });
            stack.Add(beta.NoDelay);
            stack.Add(beta.LogIO);
            stack.Add(BuildDiskRow("A", beta.DiskA));
            stack.Add(BuildDiskRow("B", beta.DiskB));
            stack.Add(BuildDiskRow("C", beta.DiskC));
            stack.Add(BuildDiskRow("D", beta.DiskD));
            return new DevicePanel(stack, beta.Apply);
        }

        private static void WireDiskBrowse(BetaDiskSettings beta)
        {
            if (beta?.Device == null)
                return;

            void OnBrowse(FileSelector fileSelector, string initialFileName)
            {
                var disks = new[] { beta.DiskA, beta.DiskB, beta.DiskC, beta.DiskD };
                var drive = Array.FindIndex(disks, d => d != null && ReferenceEquals(d.Disk, fileSelector));
                if (drive < 0 || drive >= beta.Device.LoadManagers.Length)
                    return;

                var dialog = Locator.TryResolve<IOpenFileDialog>();
                if (dialog == null)
                    return;

                using (dialog)
                {
                    dialog.Title = "Open...";
                    dialog.Filter = beta.Device.LoadManagers[drive].GetOpenExtFilter();
                    dialog.FileName = initialFileName ?? string.Empty;
                    dialog.ShowReadOnly = true;
                    dialog.ReadOnlyChecked = true;
                    dialog.CheckFileExists = true;
                    if (dialog.ShowDialog(null) != DlgResult.OK)
                        return;

                    fileSelector.SelectFile(dialog.FileName);
                    disks[drive].WriteProtect.Checked |= dialog.ReadOnlyChecked;
                }
            }

            foreach (var disk in new[] { beta.DiskA, beta.DiskB, beta.DiskC, beta.DiskD })
            {
                if (disk?.Disk == null)
                    continue;
                disk.Disk.OnBrowseFile -= OnBrowse;
                disk.Disk.OnBrowseFile += OnBrowse;
            }
        }

        private static KozuiControl BuildDiskRow(string name, Base.DiskSelector disk)
        {
            if (disk == null || !disk.Visible)
                return new Label { Text = $"Drive {name}: (n/a)", Visible = false };

            disk.Present.Text = $"{name}";
            disk.WriteProtect.Text = "WP";
            var file = new Label
            {
                Text = FileSelector.FormatDisplayFileName(disk.Disk?.FileName),
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            disk.Disk.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == null || e.PropertyName == nameof(FileSelector.FileName))
                    file.Text = FileSelector.FormatDisplayFileName(disk.Disk.FileName);
            };

            var browse = new Button { Text = "..." };
            browse.Enabled = disk.Disk.Enabled;
            disk.Disk.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == null || e.PropertyName == nameof(FileSelector.Enabled))
                    browse.Enabled = disk.Disk.Enabled;
            };
            browse.Clicked += (_, __) => disk.Disk.BrowseFile(disk.Disk.FileName);

            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 1 };
            row.Add(disk.Present);
            row.Add(disk.WriteProtect);
            row.Add(file);
            row.Add(browse);
            return row;
        }

        private static DevicePanel CreateGenericInfo(BusDeviceBase device)
        {
            var stack = new StackPanel { Orientation = Orientation.Vertical, Spacing = 1 };
            if (device == null)
            {
                stack.Add(new Label { Text = "No device selected" });
                return new DevicePanel(stack, () => { });
            }

            stack.Add(new Label { Text = device.Name ?? device.GetType().Name });
            stack.Add(new Label { Text = $"Category: {device.Category}" });
            if (!string.IsNullOrEmpty(device.Description))
            {
                foreach (var line in Wrap(device.Description, 36))
                    stack.Add(new Label { Text = line });
            }
            else
            {
                stack.Add(new Label { Text = "(no settings)" });
            }

            return new DevicePanel(stack, () => { });
        }

        private static IEnumerable<string> Wrap(string text, int width)
        {
            if (string.IsNullOrEmpty(text))
                yield break;
            var words = text.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            var line = string.Empty;
            foreach (var word in words)
            {
                if (line.Length == 0)
                {
                    line = word;
                    continue;
                }
                if (line.Length + 1 + word.Length <= width)
                {
                    line += " " + word;
                }
                else
                {
                    yield return line;
                    line = word;
                }
            }
            if (line.Length > 0)
                yield return line;
        }
    }
}
