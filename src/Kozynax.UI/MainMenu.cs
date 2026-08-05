using System;
using System.Collections.Generic;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Host.WinForms.Lib.Layout;
using ZXMAK2.Mvvm;

namespace Kozynax.UI
{
    public sealed class MenuNode
    {
        public string Caption { get; set; }
        public ICommand Command { get; set; }
        public object Parameter { get; set; }
        public Func<bool> IsChecked { get; set; }
        public List<MenuNode> Children { get; set; }
        public bool IsBack { get; set; }

        public bool HasChildren => Children != null && Children.Count > 0;
    }

    /// <summary>
    /// Hierarchical main menu as a Kozui tree (Terminal / future hosts).
    /// </summary>
    public sealed class MainMenu
    {
        private readonly object _commandParameter;
        private readonly Stack<MenuLevel> _stack = new Stack<MenuLevel>();

        public event EventHandler CloseRequested;

        /// <summary>
        /// True when the menu closed via Esc/back at the root; false when a command closed it.
        /// </summary>
        public bool ClosedByUser { get; private set; }

        public Panel Root { get; }
        public Label TitleLabel { get; }
        public ListView<MenuNode> Items { get; }

        public MainMenu(MenuNode root, object commandParameter = null)
        {
            if (root == null)
                throw new ArgumentNullException(nameof(root));
            _commandParameter = commandParameter;

            TitleLabel = new Label
            {
                Text = root.Caption ?? "Menu",
                Dock = Dock.Top,
                Margin = new Thickness(0, 0, 0, 1),
            };

            Items = new ListView<MenuNode>
            {
                ActivateOnClick = true,
                Dock = Dock.Fill,
                ItemTextSelector = FormatItem,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
            };
            Items.ItemActivated += Items_ItemActivated;

            var help = new Label
            {
                Text = "Enter/click open  Esc back/close",
                Dock = Dock.Bottom,
                Margin = new Thickness(0, 1, 0, 0),
            };

            Root = new DockPanel { Margin = new Thickness(1) };
            Root.Add(TitleLabel);
            Root.Add(help);
            Root.Add(Items);

            PushLevel(root.Caption ?? "Menu", root.Children ?? new List<MenuNode>());
        }

        public bool TryGoBack()
        {
            if (_stack.Count <= 1)
            {
                ClosedByUser = true;
                CloseRequested?.Invoke(this, EventArgs.Empty);
                return false;
            }

            _stack.Pop();
            ShowCurrent();
            return true;
        }

        private void Items_ItemActivated(object sender, EventArgs e)
        {
            var index = Items.SelectedIndex;
            if (index < 0 || index >= Items.List.Count)
                return;

            var node = Items.List[index];
            if (node == null)
                return;

            if (node.IsBack)
            {
                TryGoBack();
                return;
            }

            if (node.HasChildren)
            {
                PushLevel(node.Caption, node.Children);
                return;
            }

            if (node.Command == null)
                return;

            var param = node.Parameter ?? _commandParameter;
            if (!node.Command.CanExecute(param))
                return;

            node.Command.Execute(param);
            ShowCurrent();

            if (!IsToggleCommand(node))
            {
                ClosedByUser = false;
                CloseRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private static bool IsToggleCommand(MenuNode node)
        {
            if (node?.Command == null)
                return false;
            if (node.IsChecked != null)
                return true;
            var text = node.Command.Text ?? string.Empty;
            return text.IndexOf("Pause", StringComparison.OrdinalIgnoreCase) >= 0
                   || text.IndexOf("Resume", StringComparison.OrdinalIgnoreCase) >= 0
                   || text.IndexOf("Full Screen", StringComparison.OrdinalIgnoreCase) >= 0
                   || text.IndexOf("Windowed", StringComparison.OrdinalIgnoreCase) >= 0
                   || text.IndexOf("Maximum Speed", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void PushLevel(string title, List<MenuNode> children)
        {
            _stack.Push(new MenuLevel(title, children));
            ShowCurrent();
        }

        private void ShowCurrent()
        {
            var level = _stack.Peek();
            TitleLabel.Text = level.Title;

            var rows = new List<MenuNode>();
            if (_stack.Count > 1)
                rows.Add(new MenuNode { Caption = "..", IsBack = true });

            foreach (var child in level.Children)
            {
                if (child == null)
                    continue;
                rows.Add(child);
            }

            Items.Reset(rows);
            Items.SelectedIndex = rows.Count > 0 ? 0 : -1;
        }

        private static string FormatItem(MenuNode node)
        {
            if (node == null)
                return string.Empty;
            if (node.IsBack)
                return "..";

            var text = node.Command != null
                ? (node.Command.Text ?? node.Caption ?? string.Empty)
                : (node.Caption ?? string.Empty);

            if (node.IsChecked != null)
                return (node.IsChecked() ? "[x] " : "[ ] ") + text;
            if (node.HasChildren)
                return "  > " + text;
            return "    " + text;
        }

        private sealed class MenuLevel
        {
            public MenuLevel(string title, List<MenuNode> children)
            {
                Title = title;
                Children = children ?? new List<MenuNode>();
            }

            public string Title { get; }
            public List<MenuNode> Children { get; }
        }
    }
}
