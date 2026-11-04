using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Kozynax.UI;

namespace ZXMAK2.Host.AvaloniaBackend.Views
{
    /// <summary>
    /// Builds the Avalonia <see cref="Menu"/> from the shared <see cref="MenuNode"/> tree
    /// (<see cref="MainMenuFactory"/>). Children are regenerated on every open so command
    /// text/checked state (Pause/Resume, scale mode) is always current.
    /// </summary>
    public static class MenuBuilder
    {
        /// <param name="rootFactory">
        /// Re-invoked each time a top-level item opens; command captions (Pause/Resume)
        /// are baked into <see cref="MenuNode.Caption"/> at build time.
        /// </param>
        public static void Populate(Menu menu, Func<MenuNode> rootFactory, object commandParameter)
        {
            menu.Items.Clear();
            var root = rootFactory();
            if (root?.Children == null)
                return;

            for (var i = 0; i < root.Children.Count; i++)
            {
                var node = root.Children[i];
                if (node == null || node.IsBack)
                    continue;

                var index = i;
                var item = new MenuItem { Header = Caption(node) };
                item.Items.Add(new MenuItem { Header = "…", IsEnabled = false });
                item.SubmenuOpened += (s, e) =>
                {
                    if (!ReferenceEquals(e.Source, item))
                        return;
                    var fresh = rootFactory();
                    var freshNode = fresh?.Children != null && index < fresh.Children.Count
                        ? fresh.Children[index]
                        : node;
                    Fill(item, freshNode, commandParameter);
                };
                menu.Items.Add(item);
            }
        }

        private static MenuItem CreateItem(MenuNode node, object commandParameter)
        {
            if (node.IsBack)
                return null;

            var item = new MenuItem { Header = Caption(node) };

            if (node.HasChildren)
            {
                // Placeholder so Avalonia renders the submenu arrow; filled on open.
                item.Items.Add(new MenuItem { Header = "…", IsEnabled = false });
                item.SubmenuOpened += (s, e) =>
                {
                    if (!ReferenceEquals(e.Source, item))
                        return;
                    Fill(item, node, commandParameter);
                };
                return item;
            }

            if (node.Command == null)
            {
                item.IsEnabled = false;
                return item;
            }

            var parameter = node.Parameter ?? commandParameter;
            item.IsEnabled = SafeCanExecute(node, parameter);
            if (node.IsChecked != null)
            {
                item.ToggleType = MenuItemToggleType.CheckBox;
                item.IsChecked = SafeIsChecked(node);
            }
            item.Click += (s, e) =>
            {
                e.Handled = true;
                try
                {
                    if (node.Command.CanExecute(parameter))
                        node.Command.Execute(parameter);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            };
            return item;
        }

        private static void Fill(MenuItem parent, MenuNode node, object commandParameter)
        {
            parent.Items.Clear();
            foreach (var child in node.Children ?? new List<MenuNode>())
            {
                if (child == null)
                    continue;
                var item = CreateItem(child, commandParameter);
                if (item != null)
                    parent.Items.Add(item);
            }
            if (parent.Items.Count == 0)
                parent.Items.Add(new MenuItem { Header = "(empty)", IsEnabled = false });
        }

        private static string Caption(MenuNode node)
        {
            var text = node.Caption ?? node.Command?.Text ?? "Command";
            // Avalonia treats '_' as the access-key marker.
            return text.Replace("_", "__");
        }

        private static bool SafeCanExecute(MenuNode node, object parameter)
        {
            try
            {
                return node.Command.CanExecute(parameter);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }

        private static bool SafeIsChecked(MenuNode node)
        {
            try
            {
                return node.IsChecked();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                return false;
            }
        }
    }
}
