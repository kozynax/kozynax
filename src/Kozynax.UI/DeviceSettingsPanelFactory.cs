using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ZXMAK2;
using ZXMAK2.Engine;
using ZXMAK2.Engine.Entities;
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
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type settingsType;
                try
                {
                    settingsType = FindDeviceSettingsType(device.GetType(), asm);
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
                    var init = settingsType.GetMethod("Init", new[] { typeof(BusManager), typeof(IHostService), device.GetType() });
                    if (init == null)
                    {
                        // Try interface / base argument
                        init = settingsType.GetMethods()
                            .FirstOrDefault(m => m.Name == "Init" && m.GetParameters().Length == 3);
                    }
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
            foreach (var type in IterateTypes(deviceType))
            {
                foreach (var t in types)
                {
                    if (t == null || !t.IsClass || t.IsAbstract)
                        continue;
                    var baseType = t.BaseType;
                    if (baseType == null || !baseType.IsGenericType)
                        continue;
                    if (baseType.GetGenericTypeDefinition() != typeof(DeviceSettings<>))
                        continue;
                    var arg = baseType.GetGenericArguments()[0];
                    if (arg.IsAssignableFrom(type) || type.IsAssignableFrom(arg) || arg == type)
                        candidates.Add(t);
                }
            }

            return candidates
                .OrderByDescending(t => Specificity(t, deviceType))
                .FirstOrDefault();
        }

        private static int Specificity(Type settingsType, Type deviceType)
        {
            var arg = settingsType.BaseType.GetGenericArguments()[0];
            if (arg == deviceType)
                return 100;
            if (arg.IsAssignableFrom(deviceType))
                return 50;
            return 0;
        }

        private static IEnumerable<Type> IterateTypes(Type type)
        {
            while (type != null && type != typeof(object))
            {
                yield return type;
                foreach (var iface in type.GetInterfaces())
                    yield return iface;
                type = type.BaseType;
            }
        }

        private static DevicePanel BuildKnownPanel(object settings)
        {
            if (settings is GenericSoundSettings sound)
                return BuildSound(sound);

            if (settings is BetaDiskSettings beta)
                return BuildBetaDisk(beta);

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

        private static KozuiControl BuildDiskRow(string name, Base.DiskSelector disk)
        {
            if (disk == null || !disk.Visible)
                return new Label { Text = $"Drive {name}: (n/a)", Visible = false };

            disk.Present.Text = $"{name}";
            disk.WriteProtect.Text = "WP";
            var file = new Label
            {
                Text = TruncateFile(disk.Disk?.FileName),
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            disk.Disk.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == null || e.PropertyName == nameof(FileSelector.FileName))
                    file.Text = TruncateFile(disk.Disk.FileName);
            };

            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 1 };
            row.Add(disk.Present);
            row.Add(disk.WriteProtect);
            row.Add(file);
            return row;
        }

        private static string TruncateFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "(empty)";
            if (path.Length <= 24)
                return path;
            return "..." + path.Substring(path.Length - 21);
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
