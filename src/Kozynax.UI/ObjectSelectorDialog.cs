using System;
using System.Collections.Generic;
using Kozui.Abstract;
using ZXMAK2.Host.Entities;
using ZXMAK2.Host.WinForms.Lib;
using ZXMAK2.Host.WinForms.Lib.Layout;

namespace Kozynax.UI
{
    /// <summary>
    /// Pick one object from a list (WinForms ObjectSelectorDialog equivalent).
    /// Used when a ZIP contains multiple loadable entries.
    /// </summary>
    public class ObjectSelectorDialog : ViewDescription<ObjectSelectorDialog>
    {
        public event EventHandler CloseRequested;

        public Panel Root { get; }
        public Label TitleLabel { get; }
        public ListView<object> ItemsList { get; }
        public Button OkButton { get; }
        public Button CancelButton { get; }
        public DlgResult DialogResult { get; private set; } = DlgResult.Cancel;

        private object[] _items = Array.Empty<object>();

        public object[] ItemArray
        {
            get => _items;
            set
            {
                _items = value ?? Array.Empty<object>();
                ItemsList.Reset(_items);
                if (_items.Length > 0)
                    ItemsList.SelectedIndex = 0;
                else
                    ItemsList.SelectedIndex = -1;
            }
        }

        public object ItemSelected
        {
            get
            {
                var index = ItemsList.SelectedIndex;
                if (index < 0 || index >= _items.Length)
                    return null;
                return _items[index];
            }
            set
            {
                for (var i = 0; i < _items.Length; i++)
                {
                    if (ReferenceEquals(_items[i], value) || Equals(_items[i], value))
                    {
                        ItemsList.SelectedIndex = i;
                        return;
                    }
                }
                ItemsList.SelectedIndex = _items.Length > 0 ? 0 : -1;
            }
        }

        public ObjectSelectorDialog(string caption = null)
        {
            TitleLabel = new Label
            {
                Text = string.IsNullOrEmpty(caption) ? "Select" : caption,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            ItemsList = new ListView<object>
            {
                ItemTextSelector = FormatItem,
                ActivateOnClick = false,
                ActivateOnSecondClick = false,
                Dock = Dock.Fill,
                MinWidth = 36,
                MinHeight = 10,
                Margin = new Thickness(0, 1, 0, 1),
            };
            OkButton = new Button { Text = "OK" };
            CancelButton = new Button { Text = "Cancel" };

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 2,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            buttons.Add(OkButton);
            buttons.Add(CancelButton);

            var content = new DockPanel
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                Margin = new Thickness(2),
                MinWidth = 40,
                MinHeight = 14,
            };
            TitleLabel.Dock = Dock.Top;
            buttons.Dock = Dock.Bottom;
            ItemsList.Dock = Dock.Fill;
            content.Add(TitleLabel);
            content.Add(buttons);
            content.Add(ItemsList);

            var frame = new Placeholder
            {
                Content = content,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(2),
                MinWidth = 44,
                MinHeight = 16,
            };

            Root = new Panel();
            Root.Add(frame);

            OkButton.Clicked += (_, __) => Accept();
            CancelButton.Clicked += (_, __) => Cancel();
            ItemsList.ItemActivated += (_, __) => Accept();
        }

        public static object Select(object[] items, string caption)
        {
            if (items == null || items.Length < 1)
                return null;
            if (items.Length == 1)
                return items[0];

            var dialog = new ObjectSelectorDialog(caption);
            dialog.ItemArray = items;
            dialog.ItemSelected = items[0];
            if (dialog.ShowDialog(null) != DlgResult.OK)
                return null;
            return dialog.ItemSelected;
        }

        public void Accept()
        {
            if (ItemSelected == null)
                return;
            Complete(DlgResult.OK);
        }

        public void Cancel() => Complete(DlgResult.Cancel);

        private void Complete(DlgResult result)
        {
            DialogResult = result;
            CloseRequested?.Invoke(this, EventArgs.Empty);
        }

        private static string FormatItem(object item)
        {
            if (item == null)
                return string.Empty;
            return item.ToString() ?? string.Empty;
        }
    }
}
