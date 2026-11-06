using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.VisualTree;
using K = ZXMAK2.Host.WinForms.Lib;
using KL = ZXMAK2.Host.WinForms.Lib.Layout;

namespace ZXMAK2.Host.AvaloniaBackend.Kozui
{
    /// <summary>
    /// Maps a Kozui control tree to live Avalonia controls with two-way bindings.
    /// Kozui layout is in character cells; cells are converted to a fixed pixel grid
    /// here and the real measure/arrange is left to Avalonia.
    /// </summary>
    internal sealed class KozuiTreeBuilder : IDisposable
    {
        public const double CellW = 8;
        public const double CellH = 8;
        public const double RowH = 22;

        private readonly KozuiBindingScope _rootScope = new KozuiBindingScope();

        /// <summary>Interactive Avalonia controls in tree order (for initial focus).</summary>
        public List<Control> Focusables { get; } = new List<Control>();

        /// <summary>
        /// Cap the height of Stretch lists too. Needed for size-to-content dialogs, where an
        /// unconstrained list would otherwise grow the window to its full item count.
        /// </summary>
        public bool ConstrainStretchLists { get; set; }

        public Control Build(K.KozuiControl root)
            => Build(root, _rootScope);

        public void Dispose()
            => _rootScope.Dispose();

        #region Dispatch

        private Control Build(K.KozuiControl control, KozuiBindingScope scope)
        {
            if (control == null)
                return new Panel();

            Control view;
            switch (control)
            {
                case K.StackPanel stack:
                    view = BuildStack(stack, scope);
                    break;
                case K.DockPanel dock:
                    view = BuildDock(dock, scope);
                    break;
                case K.Panel panel:
                    view = BuildPanel(panel, scope);
                    break;
                case K.Placeholder placeholder:
                    view = BuildPlaceholder(placeholder, scope);
                    break;
                case K.Label label:
                    view = BuildLabel(label, scope);
                    break;
                case K.TextView textView:
                    view = BuildTextView(textView, scope);
                    break;
                case K.Button button:
                    view = BuildButton(button, scope);
                    break;
                case K.CheckBox checkBox:
                    view = BuildCheckBox(checkBox, scope);
                    break;
                case K.ComboBox combo:
                    view = BuildComboBox(combo, scope);
                    break;
                case K.TextBox textBox:
                    view = BuildTextBox(textBox, scope);
                    break;
                case K.ListView listView:
                    view = BuildListView(listView, scope);
                    break;
                case K.TrackBar trackBar:
                    view = BuildTrackBar(trackBar, scope);
                    break;
                case K.ProgressBar progress:
                    view = BuildProgressBar(progress, scope);
                    break;
                case K.ImageView image:
                    view = BuildImageView(image, scope);
                    break;
                case K.FileSelector selector:
                    view = BuildFileSelector(selector, scope);
                    break;
                case K.Timer _:
                    view = new Panel { IsVisible = false };
                    break;
                default:
                    view = new TextBlock { Text = "<" + control.GetType().Name + ">" };
                    break;
            }

            ApplyCommon(control, view, scope);
            return view;
        }

        private static void ApplyCommon(K.KozuiControl source, Control view, KozuiBindingScope scope)
        {
            view.HorizontalAlignment = Map(source.HorizontalAlignment);
            view.VerticalAlignment = Map(source.VerticalAlignment);
            var m = source.Margin;
            view.Margin = new Thickness(m.Left * CellW, m.Top * CellH, m.Right * CellW, m.Bottom * CellH);

            if (source.MinWidth > 0)
                view.MinWidth = Math.Max(view.MinWidth, source.MinWidth * CellW);
            if (source.MinHeight > 0 && !(source is K.ListView))
                view.MinHeight = Math.Max(view.MinHeight, source.MinHeight * CellH);

            scope.BindOneWay(source, nameof(K.KozuiControl.Enabled), () =>
            {
                if (view.IsEnabled != source.Enabled)
                    view.IsEnabled = source.Enabled;
            });
            scope.BindOneWay(source, nameof(K.KozuiControl.Visible), () =>
            {
                if (view.IsVisible != source.Visible)
                    view.IsVisible = source.Visible;
            });
        }

        private static HorizontalAlignment Map(KL.HorizontalAlignment value)
        {
            switch (value)
            {
                case KL.HorizontalAlignment.Left: return HorizontalAlignment.Left;
                case KL.HorizontalAlignment.Center: return HorizontalAlignment.Center;
                case KL.HorizontalAlignment.Right: return HorizontalAlignment.Right;
                default: return HorizontalAlignment.Stretch;
            }
        }

        private static VerticalAlignment Map(KL.VerticalAlignment value)
        {
            switch (value)
            {
                case KL.VerticalAlignment.Top: return VerticalAlignment.Top;
                case KL.VerticalAlignment.Center: return VerticalAlignment.Center;
                case KL.VerticalAlignment.Bottom: return VerticalAlignment.Bottom;
                default: return VerticalAlignment.Stretch;
            }
        }

        #endregion

        #region Containers

        /// <summary>
        /// Kozui StackPanel gives leftover space to Stretch children along the stacking
        /// axis; an Avalonia StackPanel never does, so this is a one-row/one-column Grid.
        /// </summary>
        private Control BuildStack(K.StackPanel stack, KozuiBindingScope scope)
        {
            var vertical = stack.Orientation == KL.Orientation.Vertical;
            var grid = new Grid();
            if (vertical)
                grid.RowSpacing = Math.Max(0, stack.Spacing) * CellH;
            else
                grid.ColumnSpacing = Math.Max(0, stack.Spacing) * CellW;

            var index = 0;
            foreach (var child in stack.Children)
            {
                var length = CanStretchStackMainAxis(child, vertical)
                    ? new GridLength(1, GridUnitType.Star)
                    : GridLength.Auto;
                // Star rows measure like Auto when the grid itself is unconstrained,
                // which matches LayoutEngine's unconstrained stack measure.
                if (vertical)
                    grid.RowDefinitions.Add(new RowDefinition(length));
                else
                    grid.ColumnDefinitions.Add(new ColumnDefinition(length));

                var view = Build(child, scope);
                if (vertical)
                    Grid.SetRow(view, index);
                else
                    Grid.SetColumn(view, index);
                grid.Children.Add(view);
                index++;
            }
            return grid;
        }

        /// <summary>Same rule as LayoutEngine: only fill panes grow along the stack axis.</summary>
        private static bool CanStretchStackMainAxis(K.KozuiControl child, bool vertical)
        {
            if (child is K.Label || child is K.Button || child is K.CheckBox || child is K.ComboBox || child is K.StackPanel)
                return false;
            return vertical
                ? child.VerticalAlignment == KL.VerticalAlignment.Stretch
                : child.HorizontalAlignment == KL.HorizontalAlignment.Stretch;
        }

        private Control BuildDock(K.DockPanel dock, KozuiBindingScope scope)
        {
            var panel = new DockPanel { LastChildFill = true };
            var fill = new List<Control>();
            foreach (var child in dock.Children)
            {
                var view = Build(child, scope);
                switch (child.Dock)
                {
                    case KL.Dock.Left:
                        DockPanel.SetDock(view, Dock.Left);
                        panel.Children.Add(view);
                        break;
                    case KL.Dock.Top:
                        DockPanel.SetDock(view, Dock.Top);
                        panel.Children.Add(view);
                        break;
                    case KL.Dock.Right:
                        DockPanel.SetDock(view, Dock.Right);
                        panel.Children.Add(view);
                        break;
                    case KL.Dock.Bottom:
                        DockPanel.SetDock(view, Dock.Bottom);
                        panel.Children.Add(view);
                        break;
                    default:
                        // Fill / None: occupies what is left, so it must come last.
                        fill.Add(view);
                        break;
                }
            }

            if (fill.Count == 1)
            {
                panel.Children.Add(fill[0]);
            }
            else if (fill.Count > 1)
            {
                var overlay = new Panel();
                foreach (var view in fill)
                    overlay.Children.Add(view);
                panel.Children.Add(overlay);
            }
            return panel;
        }

        private Control BuildPanel(K.Panel source, KozuiBindingScope scope)
        {
            var panel = new Panel();
            foreach (var child in source.Children)
                panel.Children.Add(Build(child, scope));
            return panel;
        }

        private Control BuildPlaceholder(K.Placeholder placeholder, KozuiBindingScope scope)
        {
            var border = new Border
            {
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(CellW, CellH),
            };
            border.Bind(Border.BorderBrushProperty, border.GetResourceObservable("SystemControlForegroundBaseMediumLowBrush"));

            KozuiBindingScope contentScope = null;
            void apply()
            {
                contentScope?.Dispose();
                contentScope = new KozuiBindingScope();
                border.Child = placeholder.Content != null ? Build(placeholder.Content, contentScope) : null;
            }

            scope.BindOneWay(placeholder, nameof(K.Placeholder.Content), apply);
            scope.Track(() => contentScope?.Dispose());
            return border;
        }

        #endregion

        #region Leaves

        private Control BuildLabel(K.Label label, KozuiBindingScope scope)
        {
            var text = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.NoWrap,
            };
            scope.BindOneWay(label, nameof(K.Label.Text), () => text.Text = label.Text ?? string.Empty);
            return text;
        }

        private Control BuildTextView(K.TextView textView, KozuiBindingScope scope)
        {
            var text = new SelectableTextBlock
            {
                TextWrapping = TextWrapping.Wrap,
            };
            scope.BindOneWay(textView, nameof(K.TextView.Text), () => text.Text = textView.Text ?? string.Empty);
            return text;
        }

        private Control BuildButton(K.Button source, KozuiBindingScope scope)
        {
            var button = new Button
            {
                MinWidth = 72,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                Padding = new Thickness(12, 5),
            };
            scope.BindOneWay(source, nameof(K.Button.Text), () => button.Content = source.Text ?? string.Empty);
            EventHandler<RoutedEventArgs> click = (s, e) =>
            {
                e.Handled = true;
                source.Click(source, EventArgs.Empty);
            };
            button.Click += click;
            scope.Track(() => button.Click -= click);
            Focusables.Add(button);
            return button;
        }

        private Control BuildCheckBox(K.CheckBox source, KozuiBindingScope scope)
        {
            var checkBox = new CheckBox { MinHeight = 0 };
            scope.BindOneWay(source, nameof(K.CheckBox.Text), () => checkBox.Content = source.Text ?? string.Empty);
            scope.BindOneWay(source, nameof(K.CheckBox.Checked), () =>
            {
                if (checkBox.IsChecked != source.Checked)
                    checkBox.IsChecked = source.Checked;
            });
            EventHandler<RoutedEventArgs> changed = (s, e) =>
            {
                var value = checkBox.IsChecked == true;
                if (source.Checked != value)
                    source.Checked = value;
            };
            checkBox.IsCheckedChanged += changed;
            scope.Track(() => checkBox.IsCheckedChanged -= changed);
            Focusables.Add(checkBox);
            return checkBox;
        }

        private Control BuildComboBox(K.ComboBox source, KozuiBindingScope scope)
        {
            var combo = new ComboBox
            {
                MinWidth = Math.Max(12, source.MinWidth) * CellW,
                HorizontalAlignment = HorizontalAlignment.Stretch,
            };
            var items = new List<string>();
            var syncing = false;

            void syncItems()
            {
                var fresh = new List<string>(source.ItemCount);
                for (var i = 0; i < source.ItemCount; i++)
                    fresh.Add(source.GetItem(i));
                if (!fresh.SequenceEqual(items))
                {
                    items = fresh;
                    syncing = true;
                    try
                    {
                        combo.ItemsSource = items.ToArray();
                    }
                    finally
                    {
                        syncing = false;
                    }
                }
                if (combo.SelectedIndex != source.SelectedIndex)
                {
                    syncing = true;
                    try
                    {
                        combo.SelectedIndex = source.SelectedIndex;
                    }
                    finally
                    {
                        syncing = false;
                    }
                }
            }

            // SetItems only raises SelectedIndex; HighlightIndex changes while browsing.
            scope.BindOneWay(source, nameof(K.ComboBox.SelectedIndex), syncItems);

            EventHandler<SelectionChangedEventArgs> selection = (s, e) =>
            {
                if (syncing)
                    return;
                if (combo.SelectedIndex >= 0 && source.SelectedIndex != combo.SelectedIndex)
                    source.SelectedIndex = combo.SelectedIndex;
            };
            combo.SelectionChanged += selection;
            scope.Track(() => combo.SelectionChanged -= selection);

            // Terminal: Enter on a closed combo opens it (instead of accepting the dialog).
            EventHandler<KeyEventArgs> keyDown = (s, e) =>
            {
                if (e.Key == Key.Enter && !combo.IsDropDownOpen)
                {
                    combo.IsDropDownOpen = true;
                    e.Handled = true;
                }
            };
            combo.KeyDown += keyDown;
            scope.Track(() => combo.KeyDown -= keyDown);
            Focusables.Add(combo);

            if (string.IsNullOrEmpty(source.ClosedPrefix))
                return combo;

            var row = new DockPanel { LastChildFill = true };
            var prefix = new TextBlock
            {
                Text = source.ClosedPrefix.TrimEnd(),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, CellW, 0),
            };
            DockPanel.SetDock(prefix, Dock.Left);
            row.Children.Add(prefix);
            row.Children.Add(combo);
            return row;
        }

        private Control BuildTextBox(K.TextBox source, KozuiBindingScope scope)
        {
            var textBox = new TextBox
            {
                MinWidth = Math.Max(16, source.MinWidth) * CellW,
                AcceptsReturn = false,
            };
            var syncing = false;
            scope.BindOneWay(source, nameof(K.TextBox.MaxLength), () => textBox.MaxLength = source.MaxLength);
            scope.BindOneWay(source, nameof(K.TextBox.Text), () =>
            {
                var text = source.Text ?? string.Empty;
                if ((textBox.Text ?? string.Empty) == text)
                    return;
                syncing = true;
                try
                {
                    textBox.Text = text;
                    textBox.CaretIndex = text.Length;
                }
                finally
                {
                    syncing = false;
                }
            });
            EventHandler<TextChangedEventArgs> changed = (s, e) =>
            {
                if (syncing)
                    return;
                var text = textBox.Text ?? string.Empty;
                if (source.Text != text)
                    source.Text = text;
            };
            textBox.TextChanged += changed;
            scope.Track(() => textBox.TextChanged -= changed);
            Focusables.Add(textBox);
            return textBox;
        }

        private Control BuildTrackBar(K.TrackBar source, KozuiBindingScope scope)
        {
            var slider = new Slider
            {
                MinWidth = Math.Max(10, source.MinWidth) * CellW,
                IsSnapToTickEnabled = true,
                TickFrequency = 1,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var syncing = false;
            void apply()
            {
                syncing = true;
                try
                {
                    if (Math.Abs(slider.Minimum - source.Minimum) > double.Epsilon)
                        slider.Minimum = source.Minimum;
                    if (Math.Abs(slider.Maximum - source.Maximum) > double.Epsilon)
                        slider.Maximum = source.Maximum;
                    var value = Math.Max(source.Minimum, Math.Min(source.Maximum, source.Value));
                    if (Math.Abs(slider.Value - value) > double.Epsilon)
                        slider.Value = value;
                }
                finally
                {
                    syncing = false;
                }
            }
            scope.BindOneWay(source, nameof(K.TrackBar.Minimum), apply);
            scope.BindOneWay(source, nameof(K.TrackBar.Maximum), apply);
            scope.BindOneWay(source, nameof(K.TrackBar.Value), apply);
            EventHandler<RangeBaseValueChangedEventArgs> changed = (s, e) =>
            {
                if (syncing)
                    return;
                var value = (int)Math.Round(slider.Value);
                if (source.Value != value)
                    source.Value = value;
            };
            slider.ValueChanged += changed;
            scope.Track(() => slider.ValueChanged -= changed);
            Focusables.Add(slider);
            return slider;
        }

        private Control BuildProgressBar(K.ProgressBar source, KozuiBindingScope scope)
        {
            // Fixed height so a Stretch bar in a button row stays a slim bar (centred).
            var bar = new ProgressBar
            {
                MinWidth = Math.Max(8, source.MinWidth) * CellW,
                Height = 14,
            };
            void apply()
            {
                bar.Minimum = source.Minimum;
                bar.Maximum = Math.Max(source.Minimum, source.Maximum);
                bar.Value = Math.Max(bar.Minimum, Math.Min(bar.Maximum, source.Value));
            }
            scope.BindOneWay(source, nameof(K.ProgressBar.Minimum), apply);
            scope.BindOneWay(source, nameof(K.ProgressBar.Maximum), apply);
            scope.BindOneWay(source, nameof(K.ProgressBar.Value), apply);
            return bar;
        }

        private Control BuildImageView(K.ImageView source, KozuiBindingScope scope)
        {
            var image = new Image
            {
                Stretch = Stretch.Uniform,
                StretchDirection = StretchDirection.DownOnly,
            };
            scope.BindOneWay(source, nameof(K.ImageView.Image), () =>
            {
                (image.Source as IDisposable)?.Dispose();
                image.Source = null;
                var open = source.Image;
                if (open == null)
                    return;
                try
                {
                    using (var stream = open())
                    {
                        if (stream != null)
                            image.Source = new Bitmap(stream);
                    }
                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }
            });
            scope.Track(() =>
            {
                (image.Source as IDisposable)?.Dispose();
                image.Source = null;
            });
            return image;
        }

        private Control BuildFileSelector(K.FileSelector source, KozuiBindingScope scope)
        {
            var name = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
            var browse = new Button { Content = "...", MinWidth = 40, Padding = new Thickness(8, 4) };
            scope.BindOneWay(source, nameof(K.FileSelector.FileName),
                () => name.Text = K.FileSelector.FormatDisplayFileName(source.FileName, 40));
            EventHandler<RoutedEventArgs> click = (s, e) =>
            {
                e.Handled = true;
                source.BrowseFile(source.FileName);
            };
            browse.Click += click;
            scope.Track(() => browse.Click -= click);
            Focusables.Add(browse);

            var row = new DockPanel { LastChildFill = true };
            DockPanel.SetDock(browse, Dock.Right);
            row.Children.Add(browse);
            row.Children.Add(name);
            return row;
        }

        #endregion

        #region ListView

        private Control BuildListView(K.ListView source, KozuiBindingScope scope)
        {
            var rows = new ObservableCollection<ListRow>();
            var listBox = new ListBox
            {
                ItemsSource = rows,
                SelectionMode = SelectionMode.Single,
                MinWidth = Math.Max(10, source.MinWidth) * CellW,
                ItemTemplate = new FuncDataTemplate<ListRow>((row, _) => CreateRowView(row), supportsRecycling: false),
            };
            // Dense rows: the Fluent default padding is sized for touch.
            listBox.Styles.Add(new Style(x => x.OfType<ListBoxItem>())
            {
                Setters =
                {
                    new Setter(TemplatedControl.PaddingProperty, new Thickness(8, 2)),
                    new Setter(Layoutable.MinHeightProperty, RowH),
                },
            });

            // MinHeight is a fixed viewport in rows. Pin Height/Width so virtualization
            // (different realized row lengths while scrolling) cannot resize the dialog.
            var viewportRows = source.MinHeight > 0 ? source.MinHeight : 0;
            if (viewportRows > 0)
            {
                var height = viewportRows * RowH + 4;
                listBox.Height = height;
                listBox.MinHeight = height;
                listBox.MaxHeight = height;
            }
            else if (source.VerticalAlignment != KL.VerticalAlignment.Stretch || ConstrainStretchLists)
            {
                listBox.MaxHeight = 16 * RowH + 4;
            }

            if (source.MinWidth > 0 && ConstrainStretchLists)
            {
                var width = Math.Max(10, source.MinWidth) * CellW;
                listBox.Width = width;
                listBox.MinWidth = width;
                listBox.MaxWidth = width;
            }

            var syncing = false;

            void syncRows()
            {
                var count = source.Count;
                while (rows.Count > count)
                    rows.RemoveAt(rows.Count - 1);
                for (var i = 0; i < count; i++)
                {
                    var text = source.GetItemText(i) ?? string.Empty;
                    var colors = source.GetRowColors?.Invoke(i);
                    if (i < rows.Count)
                        rows[i].Update(text, colors);
                    else
                        rows.Add(new ListRow(text, colors));
                }
            }

            void syncSelection()
            {
                var selected = source.SelectedIndex;
                if (selected < 0 || selected >= rows.Count)
                    selected = -1;
                if (listBox.SelectedIndex == selected)
                    return;
                syncing = true;
                try
                {
                    listBox.SelectedIndex = selected;
                    if (selected >= 0)
                        listBox.ScrollIntoView(rows[selected]);
                }
                finally
                {
                    syncing = false;
                }
            }

            syncRows();
            syncSelection();

            // ListView<T>.List is a BindingList; the non-generic base exposes no change event.
            var listProp = source.GetType().GetProperty("List");
            if (listProp?.GetValue(source) is IBindingList bindingList)
            {
                ListChangedEventHandler listChanged = (s, e) =>
                {
                    syncRows();
                    syncSelection();
                };
                bindingList.ListChanged += listChanged;
                scope.Track(() => bindingList.ListChanged -= listChanged);
            }
            scope.BindOneWay(source, nameof(K.ListView.SelectedIndex), syncSelection);

            EventHandler<SelectionChangedEventArgs> selection = (s, e) =>
            {
                if (syncing)
                    return;
                var index = listBox.SelectedIndex;
                if (index >= 0 && source.SelectedIndex != index)
                    source.SelectedIndex = index;
            };
            listBox.SelectionChanged += selection;
            scope.Track(() => listBox.SelectionChanged -= selection);

            // Activation: Enter, double-click, or click rules mirrored from the Terminal presenter.
            EventHandler<KeyEventArgs> keyDown = (s, e) =>
            {
                if (e.Key == Key.Enter && source.Count > 0)
                {
                    e.Handled = true;
                    source.ActivateItem();
                }
            };
            listBox.KeyDown += keyDown;
            scope.Track(() => listBox.KeyDown -= keyDown);

            var pressWasSelected = false;
            var pressIndex = -1;
            EventHandler<PointerPressedEventArgs> pressed = (s, e) =>
            {
                pressIndex = IndexAt(listBox, e.Source);
                pressWasSelected = pressIndex >= 0 && pressIndex == listBox.SelectedIndex;
            };
            EventHandler<PointerReleasedEventArgs> released = (s, e) =>
            {
                if (e.InitialPressMouseButton != MouseButton.Left)
                    return;
                var index = IndexAt(listBox, e.Source);
                if (index < 0 || index != pressIndex || index != source.SelectedIndex)
                    return;
                if (source.ActivateOnClick || (source.ActivateOnSecondClick && pressWasSelected))
                {
                    e.Handled = true;
                    source.ActivateItem();
                }
            };
            listBox.AddHandler(InputElement.PointerPressedEvent, pressed, RoutingStrategies.Tunnel);
            listBox.AddHandler(InputElement.PointerReleasedEvent, released, RoutingStrategies.Bubble);
            scope.Track(() =>
            {
                listBox.RemoveHandler(InputElement.PointerPressedEvent, pressed);
                listBox.RemoveHandler(InputElement.PointerReleasedEvent, released);
            });

            if (!source.ActivateOnClick && !source.ActivateOnSecondClick)
            {
                EventHandler<TappedEventArgs> doubleTapped = (s, e) =>
                {
                    if (IndexAt(listBox, e.Source) >= 0 && source.SelectedIndex >= 0)
                    {
                        e.Handled = true;
                        source.ActivateItem();
                    }
                };
                listBox.DoubleTapped += doubleTapped;
                scope.Track(() => listBox.DoubleTapped -= doubleTapped);
            }

            // Visibility toggles do not re-measure a collapsed Grid star row reliably; refresh on show.
            scope.BindOneWay(source, nameof(K.KozuiControl.Visible), () =>
            {
                if (source.Visible)
                    syncRows();
            });

            Focusables.Add(listBox);
            return listBox;
        }

        private static int IndexAt(ListBox listBox, object eventSource)
        {
            var item = (eventSource as Visual)?.FindAncestorOfType<ListBoxItem>(true);
            if (item == null)
                return -1;
            return listBox.IndexFromContainer(item);
        }

        private static Control CreateRowView(ListRow row)
        {
            var text = new TextBlock
            {
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Padding = new Thickness(2, 0),
            };
            // Container recycling re-runs the template with null content.
            if (row == null)
                return text;

            void apply()
            {
                text.Text = row.Text;
                text.Background = row.Background;
                if (row.Foreground != null)
                    text.Foreground = row.Foreground;
                else
                    text.ClearValue(TextBlock.ForegroundProperty);
            }
            apply();
            PropertyChangedEventHandler handler = (s, e) => apply();
            row.PropertyChanged += handler;
            text.DetachedFromVisualTree += (s, e) => row.PropertyChanged -= handler;
            text.AttachedToVisualTree += (s, e) =>
            {
                row.PropertyChanged -= handler;
                row.PropertyChanged += handler;
                apply();
            };
            return text;
        }

        private sealed class ListRow : INotifyPropertyChanged
        {
            public ListRow(string text, K.ListRowColors? colors)
            {
                Text = text;
                SetColors(colors);
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public string Text { get; private set; }
            public IBrush Background { get; private set; }
            public IBrush Foreground { get; private set; }

            public void Update(string text, K.ListRowColors? colors)
            {
                var changed = Text != text;
                Text = text;
                changed |= SetColors(colors);
                if (changed)
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
            }

            private bool SetColors(K.ListRowColors? colors)
            {
                IBrush bg = null;
                IBrush fg = null;
                if (colors.HasValue)
                {
                    var c = colors.Value;
                    bg = new SolidColorBrush(Color.FromRgb(c.BgR, c.BgG, c.BgB));
                    fg = new SolidColorBrush(Color.FromRgb(c.FgR, c.FgG, c.FgB));
                }
                var changed = !BrushEquals(Background, bg) || !BrushEquals(Foreground, fg);
                Background = bg;
                Foreground = fg;
                return changed;
            }

            private static bool BrushEquals(IBrush a, IBrush b)
            {
                if (ReferenceEquals(a, b))
                    return true;
                if (a is ISolidColorBrush sa && b is ISolidColorBrush sb)
                    return sa.Color == sb.Color;
                return false;
            }

            public override string ToString() => Text;
        }

        #endregion
    }
}
