using System;
using System.Collections.Generic;

namespace ZXMAK2.Host.WinForms.Lib.Layout
{
    /// <summary>
    /// Measure/arrange in logical character-cell units.
    /// Hosts supply available size; presenters only paint ArrangedBounds.
    /// </summary>
    public static class LayoutEngine
    {
        public static void MeasureArrange(KozuiControl root, LayoutSize available)
        {
            if (root == null)
                return;
            Measure(root, available);
            Arrange(root, new LayoutRect(0, 0, available.Width, available.Height));
        }

        public static LayoutSize Measure(KozuiControl control, LayoutSize available)
        {
            if (control == null || !control.Visible)
            {
                if (control != null)
                    control.DesiredSize = LayoutSize.Empty;
                return LayoutSize.Empty;
            }

            var margin = control.Margin;
            var innerAvail = new LayoutSize(
                Math.Max(0, available.Width - margin.Horizontal),
                Math.Max(0, available.Height - margin.Vertical));

            LayoutSize content;
            if (control is StackPanel stack)
                content = MeasureStack(stack, innerAvail);
            else if (control is DockPanel dock)
                content = MeasureDock(dock, innerAvail);
            else if (control is Panel panel)
                content = MeasurePanelFill(panel, innerAvail);
            else if (control is Placeholder placeholder)
                content = MeasurePlaceholder(placeholder, innerAvail);
            else
                content = MeasureLeaf(control, innerAvail);

            var width = Math.Max(control.MinWidth, content.Width) + margin.Horizontal;
            var height = Math.Max(control.MinHeight, content.Height) + margin.Vertical;
            control.DesiredSize = new LayoutSize(width, height);
            return control.DesiredSize;
        }

        public static void Arrange(KozuiControl control, LayoutRect finalRect)
        {
            if (control == null || !control.Visible)
            {
                if (control != null)
                    control.ArrangedBounds = LayoutRect.Empty;
                return;
            }

            var margin = control.Margin;
            var inner = new LayoutRect(
                finalRect.X + margin.Left,
                finalRect.Y + margin.Top,
                Math.Max(0, finalRect.Width - margin.Horizontal),
                Math.Max(0, finalRect.Height - margin.Vertical));

            control.ArrangedBounds = inner;

            if (control is StackPanel stack)
                ArrangeStack(stack, inner);
            else if (control is DockPanel dock)
                ArrangeDock(dock, inner);
            else if (control is Panel panel)
                ArrangePanelFill(panel, inner);
            else if (control is Placeholder placeholder && placeholder.Content != null)
            {
                // Keep content clear of panel chrome / outline.
                const int pad = 1;
                Arrange(placeholder.Content, new LayoutRect(
                    inner.X + pad,
                    inner.Y + pad,
                    Math.Max(0, inner.Width - pad * 2),
                    Math.Max(0, inner.Height - pad * 2)));
            }
        }

        private static LayoutSize MeasureStack(StackPanel stack, LayoutSize available)
        {
            var spacing = Math.Max(0, stack.Spacing);
            var width = 0;
            var height = 0;
            var visibleCount = 0;

            if (stack.Orientation == Orientation.Vertical)
            {
                var childAvail = new LayoutSize(available.Width, int.MaxValue / 4);
                foreach (var child in stack.Children)
                {
                    if (!child.Visible)
                        continue;
                    var size = Measure(child, childAvail);
                    width = Math.Max(width, size.Width);
                    height += size.Height;
                    visibleCount++;
                }
                if (visibleCount > 1)
                    height += spacing * (visibleCount - 1);
            }
            else
            {
                var childAvail = new LayoutSize(int.MaxValue / 4, available.Height);
                foreach (var child in stack.Children)
                {
                    if (!child.Visible)
                        continue;
                    var size = Measure(child, childAvail);
                    height = Math.Max(height, size.Height);
                    width += size.Width;
                    visibleCount++;
                }
                if (visibleCount > 1)
                    width += spacing * (visibleCount - 1);
            }

            return new LayoutSize(width, height);
        }

        private static void ArrangeStack(StackPanel stack, LayoutRect inner)
        {
            var spacing = Math.Max(0, stack.Spacing);
            if (stack.Orientation == Orientation.Vertical)
            {
                var visible = new List<KozuiControl>();
                foreach (var child in stack.Children)
                {
                    if (child.Visible)
                        visible.Add(child);
                }

                var fixedHeight = 0;
                var stretchCount = 0;
                for (var i = 0; i < visible.Count; i++)
                {
                    var child = visible[i];
                    if (CanStretchStackMainAxis(child, vertical: true))
                        stretchCount++;
                    else
                        fixedHeight += child.DesiredSize.Height;
                }

                var spacingTotal = visible.Count > 1 ? spacing * (visible.Count - 1) : 0;
                var leftover = Math.Max(0, inner.Height - fixedHeight - spacingTotal);
                var stretchBase = stretchCount > 0 ? leftover / stretchCount : 0;
                var stretchRem = stretchCount > 0 ? leftover % stretchCount : 0;

                var y = inner.Y;
                for (var i = 0; i < visible.Count; i++)
                {
                    var child = visible[i];
                    var desired = child.DesiredSize;
                    int childHeight;
                    if (CanStretchStackMainAxis(child, vertical: true))
                    {
                        childHeight = Math.Max(desired.Height, stretchBase);
                        if (stretchRem > 0)
                        {
                            childHeight++;
                            stretchRem--;
                        }
                    }
                    else
                    {
                        childHeight = desired.Height;
                    }

                    var childWidth = AlignWidth(child, desired.Width, inner.Width);
                    var x = AlignX(child, childWidth, inner.X, inner.Width);
                    Arrange(child, new LayoutRect(x, y, childWidth, childHeight));
                    y += childHeight + spacing;
                }
            }
            else
            {
                var visible = new List<KozuiControl>();
                foreach (var child in stack.Children)
                {
                    if (child.Visible)
                        visible.Add(child);
                }

                var fixedWidth = 0;
                var stretchCount = 0;
                for (var i = 0; i < visible.Count; i++)
                {
                    var child = visible[i];
                    if (CanStretchStackMainAxis(child, vertical: false))
                        stretchCount++;
                    else
                        fixedWidth += child.DesiredSize.Width;
                }

                var spacingTotal = visible.Count > 1 ? spacing * (visible.Count - 1) : 0;
                var leftover = Math.Max(0, inner.Width - fixedWidth - spacingTotal);
                var stretchBase = stretchCount > 0 ? leftover / stretchCount : 0;
                var stretchRem = stretchCount > 0 ? leftover % stretchCount : 0;

                var x = inner.X;
                for (var i = 0; i < visible.Count; i++)
                {
                    var child = visible[i];
                    var desired = child.DesiredSize;
                    int childWidth;
                    if (CanStretchStackMainAxis(child, vertical: false))
                    {
                        childWidth = Math.Max(desired.Width, stretchBase);
                        if (stretchRem > 0)
                        {
                            childWidth++;
                            stretchRem--;
                        }
                    }
                    else
                    {
                        childWidth = desired.Width;
                    }

                    var childHeight = AlignHeight(child, desired.Height, inner.Height);
                    var y = AlignY(child, childHeight, inner.Y, inner.Height);
                    Arrange(child, new LayoutRect(x, y, childWidth, childHeight));
                    x += childWidth + spacing;
                }
            }
        }

        /// <summary>
        /// Labels/buttons default to Stretch but must not eat leftover stack space;
        /// only content panes (lists, etc.) grow along the stack main axis.
        /// </summary>
        private static bool CanStretchStackMainAxis(KozuiControl child, bool vertical)
        {
            if (child is Label || child is Button || child is CheckBox)
                return false;
            return vertical
                ? child.VerticalAlignment == VerticalAlignment.Stretch
                : child.HorizontalAlignment == HorizontalAlignment.Stretch;
        }

        private static LayoutSize MeasureDock(DockPanel dock, LayoutSize available)
        {
            // Measure docked children with axis-constrained availability so a
            // Left list does not claim the full width during measure.
            var remaining = available;
            foreach (var child in dock.Children)
            {
                if (!child.Visible)
                    continue;

                var dockSide = child.Dock;
                LayoutSize childAvail;
                switch (dockSide)
                {
                    case Dock.Left:
                    case Dock.Right:
                        childAvail = new LayoutSize(remaining.Width, remaining.Height);
                        Measure(child, childAvail);
                        remaining = new LayoutSize(
                            Math.Max(0, remaining.Width - child.DesiredSize.Width),
                            remaining.Height);
                        break;
                    case Dock.Top:
                    case Dock.Bottom:
                        childAvail = new LayoutSize(remaining.Width, remaining.Height);
                        Measure(child, childAvail);
                        remaining = new LayoutSize(
                            remaining.Width,
                            Math.Max(0, remaining.Height - child.DesiredSize.Height));
                        break;
                    default:
                        Measure(child, remaining);
                        break;
                }
            }
            return AggregateDockDesired(dock);
        }

        private static LayoutSize AggregateDockDesired(DockPanel dock)
        {
            var width = 0;
            var height = 0;
            foreach (var child in dock.Children)
            {
                if (!child.Visible)
                    continue;
                var size = child.DesiredSize;
                switch (child.Dock)
                {
                    case Dock.Left:
                    case Dock.Right:
                        width += size.Width;
                        height = Math.Max(height, size.Height);
                        break;
                    case Dock.Top:
                    case Dock.Bottom:
                        height += size.Height;
                        width = Math.Max(width, size.Width);
                        break;
                    default:
                        width = Math.Max(width, size.Width);
                        height = Math.Max(height, size.Height);
                        break;
                }
            }
            return new LayoutSize(width, height);
        }

        private static void ArrangeDock(DockPanel dock, LayoutRect inner)
        {
            var remaining = inner;
            var children = dock.Children;
            for (var i = 0; i < children.Count; i++)
            {
                var child = children[i];
                if (!child.Visible)
                    continue;

                var isLast = i == children.Count - 1;
                var dockSide = child.Dock;
                if (dockSide == Dock.None && dock.LastChildFill && isLast)
                    dockSide = Dock.Fill;

                var desired = child.DesiredSize;
                LayoutRect slot;
                switch (dockSide)
                {
                    case Dock.Left:
                    {
                        var w = Math.Min(desired.Width, remaining.Width);
                        // Preserve space for trailing Fill siblings.
                        if (!isLast && remaining.Width > 16)
                            w = Math.Min(w, remaining.Width - 16);
                        slot = new LayoutRect(remaining.X, remaining.Y, w, remaining.Height);
                        remaining = new LayoutRect(remaining.X + w, remaining.Y, remaining.Width - w, remaining.Height);
                        break;
                    }
                    case Dock.Right:
                    {
                        var w = Math.Min(desired.Width, remaining.Width);
                        if (!isLast && remaining.Width > 16)
                            w = Math.Min(w, remaining.Width - 16);
                        slot = new LayoutRect(remaining.Right - w, remaining.Y, w, remaining.Height);
                        remaining = new LayoutRect(remaining.X, remaining.Y, remaining.Width - w, remaining.Height);
                        break;
                    }
                    case Dock.Top:
                    {
                        var h = Math.Min(desired.Height, remaining.Height);
                        slot = new LayoutRect(remaining.X, remaining.Y, remaining.Width, h);
                        remaining = new LayoutRect(remaining.X, remaining.Y + h, remaining.Width, remaining.Height - h);
                        break;
                    }
                    case Dock.Bottom:
                    {
                        var h = Math.Min(desired.Height, remaining.Height);
                        slot = new LayoutRect(remaining.X, remaining.Bottom - h, remaining.Width, h);
                        remaining = new LayoutRect(remaining.X, remaining.Y, remaining.Width, remaining.Height - h);
                        break;
                    }
                    default:
                        slot = remaining;
                        remaining = LayoutRect.Empty;
                        break;
                }

                Arrange(child, slot);
            }
        }

        private static LayoutSize MeasurePanelFill(Panel panel, LayoutSize available)
        {
            var width = 0;
            var height = 0;
            foreach (var child in panel.Children)
            {
                if (!child.Visible)
                    continue;
                var size = Measure(child, available);
                width = Math.Max(width, size.Width);
                height = Math.Max(height, size.Height);
            }
            return new LayoutSize(width, height);
        }

        private static void ArrangePanelFill(Panel panel, LayoutRect inner)
        {
            foreach (var child in panel.Children)
            {
                if (!child.Visible)
                    continue;
                var desired = child.DesiredSize;
                var childWidth = AlignWidth(child, desired.Width, inner.Width);
                var childHeight = AlignHeight(child, desired.Height, inner.Height);
                var x = AlignX(child, childWidth, inner.X, inner.Width);
                var y = AlignY(child, childHeight, inner.Y, inner.Height);
                Arrange(child, new LayoutRect(x, y, childWidth, childHeight));
            }
        }

        private static LayoutSize MeasurePlaceholder(Placeholder placeholder, LayoutSize available)
        {
            if (placeholder.Content == null || !placeholder.Content.Visible)
                return new LayoutSize(placeholder.MinWidth, placeholder.MinHeight);
            const int pad = 1;
            var inner = new LayoutSize(
                Math.Max(0, available.Width - pad * 2),
                Math.Max(0, available.Height - pad * 2));
            var content = Measure(placeholder.Content, inner);
            var width = content.Width + pad * 2;
            var height = content.Height + pad * 2;
            // Stretch placeholders (e.g. full-screen file picker) claim the slot.
            if (placeholder.HorizontalAlignment == HorizontalAlignment.Stretch
                && available.Width < int.MaxValue / 8)
                width = Math.Max(width, available.Width);
            if (placeholder.VerticalAlignment == VerticalAlignment.Stretch
                && available.Height < int.MaxValue / 8)
                height = Math.Max(height, available.Height);
            return new LayoutSize(width, height);
        }

        private static LayoutSize MeasureLeaf(KozuiControl control, LayoutSize available)
        {
            if (control is Label label)
            {
                var text = label.Text ?? string.Empty;
                return new LayoutSize(Math.Max(1, text.Length), 1);
            }

            if (control is Button button)
            {
                var text = button.Text ?? string.Empty;
                // "[ text ]"
                return new LayoutSize(Math.Max(3, text.Length + 4), 1);
            }

            if (control is TextView textView)
            {
                var text = textView.Text ?? string.Empty;
                return new LayoutSize(Math.Max(1, text.Length), 1);
            }

            if (control is TextBox textBox)
            {
                return new LayoutSize(
                    Math.Max(8, textBox.MinWidth > 0 ? textBox.MinWidth : 16),
                    1);
            }

            if (control is CheckBox checkBox)
            {
                var text = checkBox.Text ?? string.Empty;
                // "[x] text"
                return new LayoutSize(Math.Max(4, text.Length + 4), 1);
            }

            if (control is ProgressBar)
            {
                return new LayoutSize(
                    Math.Max(8, control.MinWidth > 0 ? control.MinWidth : 16),
                    Math.Max(1, control.MinHeight > 0 ? control.MinHeight : 1));
            }

            if (control is TrackBar)
            {
                return new LayoutSize(
                    Math.Max(10, control.MinWidth > 0 ? control.MinWidth : 16),
                    Math.Max(1, control.MinHeight > 0 ? control.MinHeight : 1));
            }

            if (control is ListView listView)
            {
                // Width is content-sized (not available.Width) so Left/Right dock
                // does not consume the entire slot and crush Fill siblings.
                var width = Math.Max(10, control.MinWidth > 0 ? control.MinWidth : 20);
                var count = listView.Count;
                for (var i = 0; i < count; i++)
                {
                    var text = listView.GetItemText(i);
                    if (!string.IsNullOrEmpty(text))
                        width = Math.Max(width, Math.Min(40, text.Length + 1));
                }

                // MinHeight is a fixed viewport (includes 1-cell panel chrome top+bottom),
                // not a floor that grows with item count — otherwise a long list inside a
                // vertical StackPanel (unconstrained measure) overlaps siblings below.
                int rows;
                if (available.Height < int.MaxValue / 8)
                {
                    var minRows = control.MinHeight > 0 ? control.MinHeight : 3;
                    var natural = Math.Max(minRows, count > 0 ? count : minRows);
                    rows = Math.Max(minRows, Math.Min(natural, available.Height));
                }
                else if (control.MinHeight > 0)
                {
                    rows = control.MinHeight;
                }
                else
                {
                    rows = Math.Max(3, count > 0 ? count : 3);
                }

                return new LayoutSize(width, rows);
            }

            if (control is ImageView)
            {
                return new LayoutSize(
                    Math.Max(8, control.MinWidth > 0 ? control.MinWidth : 20),
                    Math.Max(4, control.MinHeight > 0 ? control.MinHeight : 8));
            }

            return new LayoutSize(
                Math.Max(1, control.MinWidth),
                Math.Max(1, control.MinHeight));
        }

        private static int AlignWidth(KozuiControl child, int desiredWidth, int slotWidth)
        {
            if (child.HorizontalAlignment == HorizontalAlignment.Stretch)
                return slotWidth;
            return Math.Min(desiredWidth, slotWidth);
        }

        private static int AlignHeight(KozuiControl child, int desiredHeight, int slotHeight)
        {
            if (child.VerticalAlignment == VerticalAlignment.Stretch)
                return slotHeight;
            return Math.Min(desiredHeight, slotHeight);
        }

        private static int AlignX(KozuiControl child, int childWidth, int slotX, int slotWidth)
        {
            switch (child.HorizontalAlignment)
            {
                case HorizontalAlignment.Center:
                    return slotX + Math.Max(0, (slotWidth - childWidth) / 2);
                case HorizontalAlignment.Right:
                    return slotX + Math.Max(0, slotWidth - childWidth);
                default:
                    return slotX;
            }
        }

        private static int AlignY(KozuiControl child, int childHeight, int slotY, int slotHeight)
        {
            switch (child.VerticalAlignment)
            {
                case VerticalAlignment.Center:
                    return slotY + Math.Max(0, (slotHeight - childHeight) / 2);
                case VerticalAlignment.Bottom:
                    return slotY + Math.Max(0, slotHeight - childHeight);
                default:
                    return slotY;
            }
        }
    }
}
