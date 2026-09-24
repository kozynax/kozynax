using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows.Forms;
using Lib = ZXMAK2.Host.WinForms.Lib;

namespace ZXMAK2.Host.WinForms.BindingTools
{
    /// <summary>
    /// One-way / two-way sync between Kozui controls (INPC) and WinForms widgets.
    /// Prefer this over ad-hoc Redraw handlers for property mirroring.
    /// </summary>
    public sealed class KozuiBinder : IDisposable
    {
        private readonly List<Action> _disposers = new List<Action>();
        private bool _disposed;

        public void BindEnabled(Lib.KozuiControl source, Control target)
        {
            BindOneWay(source, nameof(Lib.KozuiControl.Enabled), () =>
            {
                if (target.Enabled != source.Enabled)
                    target.Enabled = source.Enabled;
            });
        }

        public void BindEnabled(Lib.KozuiControl source, ToolStripItem target)
        {
            BindOneWay(source, nameof(Lib.KozuiControl.Enabled), () =>
            {
                if (target.Enabled != source.Enabled)
                    target.Enabled = source.Enabled;
            });
        }

        public void BindVisible(Lib.KozuiControl source, Control target)
        {
            BindOneWay(source, nameof(Lib.KozuiControl.Visible), () =>
            {
                if (target.Visible != source.Visible)
                    target.Visible = source.Visible;
            });
        }

        public void BindVisible(Lib.KozuiControl source, ToolStripItem target)
        {
            BindOneWay(source, nameof(Lib.KozuiControl.Visible), () =>
            {
                if (target.Visible != source.Visible)
                    target.Visible = source.Visible;
            });
        }

        /// <summary>
        /// Mirrors Enabled/Visible. Click remains host-owned unless <paramref name="bindClick"/> is true.
        /// </summary>
        public void BindButton(Lib.Button source, Button target, bool bindClick = false)
        {
            BindEnabled(source, target);
            BindVisible(source, target);
            if (bindClick)
            {
                EventHandler handler = (s, e) => source.Click(s, e);
                target.Click += handler;
                _disposers.Add(() => target.Click -= handler);
            }
        }

        public void BindButton(Lib.Button source, ToolStripItem target, bool bindClick = false)
        {
            BindEnabled(source, target);
            BindVisible(source, target);
            if (bindClick)
            {
                EventHandler handler = (s, e) => source.Click(s, e);
                target.Click += handler;
                _disposers.Add(() => target.Click -= handler);
            }
        }

        public void BindCheckBox(Lib.CheckBox source, CheckBox target, bool twoWay = true)
        {
            BindEnabled(source, target);
            BindVisible(source, target);
            BindOneWay(source, nameof(Lib.CheckBox.Checked), () =>
            {
                if (target.Checked != source.Checked)
                    target.Checked = source.Checked;
            });

            if (!twoWay)
                return;

            EventHandler handler = (s, e) =>
            {
                if (source.Checked != target.Checked)
                    source.Checked = target.Checked;
            };
            target.CheckedChanged += handler;
            _disposers.Add(() => target.CheckedChanged -= handler);
        }

        public void BindCheckBox(Lib.CheckBox source, ToolStripButton target, bool twoWay = true)
        {
            BindEnabled(source, target);
            BindVisible(source, target);
            BindOneWay(source, nameof(Lib.CheckBox.Checked), () =>
            {
                if (target.Checked != source.Checked)
                    target.Checked = source.Checked;
            });

            if (!twoWay)
                return;

            EventHandler handler = (s, e) =>
            {
                if (source.Checked != target.Checked)
                    source.Checked = target.Checked;
            };
            target.CheckedChanged += handler;
            _disposers.Add(() => target.CheckedChanged -= handler);
        }

        public void BindText(Lib.TextView source, Control target)
        {
            BindOneWay(source, nameof(Lib.TextView.Text), () =>
            {
                var text = source.Text ?? string.Empty;
                if (target.Text != text)
                    target.Text = text;
            });
        }

        public void BindText(Lib.Label source, Control target)
        {
            BindOneWay(source, nameof(Lib.Label.Text), () =>
            {
                var text = source.Text ?? string.Empty;
                if (target.Text != text)
                    target.Text = text;
            });
        }

        public void BindText(Lib.FileSelector source, Control target)
        {
            BindEnabled(source, target);
            BindVisible(source, target);
            BindOneWay(source, nameof(Lib.FileSelector.FileName), () =>
            {
                var text = Lib.FileSelector.FormatDisplayFileName(source.FileName);
                if (target.Text != text)
                    target.Text = text;
            });
        }

        public void BindTrackBar(Lib.TrackBar source, TrackBar target, bool twoWay = true)
        {
            BindEnabled(source, target);
            BindVisible(source, target);

            void applyRangeAndValue()
            {
                if (target.Minimum != source.Minimum)
                    target.Minimum = source.Minimum;
                if (target.Maximum != source.Maximum)
                    target.Maximum = source.Maximum;
                var value = Math.Max(target.Minimum, Math.Min(target.Maximum, source.Value));
                if (target.Value != value)
                    target.Value = value;
            }

            BindOneWay(source, nameof(Lib.TrackBar.Minimum), applyRangeAndValue);
            BindOneWay(source, nameof(Lib.TrackBar.Maximum), applyRangeAndValue);
            BindOneWay(source, nameof(Lib.TrackBar.Value), applyRangeAndValue);

            if (!twoWay)
                return;

            EventHandler handler = (s, e) =>
            {
                if (source.Value != target.Value)
                    source.Value = target.Value;
            };
            target.ValueChanged += handler;
            _disposers.Add(() => target.ValueChanged -= handler);
        }

        public void BindProgressBar(Lib.ProgressBar source, ToolStripProgressBar target)
        {
            BindVisible(source, target);

            void apply()
            {
                if (target.Minimum != source.Minimum)
                    target.Minimum = source.Minimum;
                if (target.Maximum != source.Maximum)
                    target.Maximum = source.Maximum;
                var value = Math.Max(target.Minimum, Math.Min(target.Maximum, source.Value));
                if (target.Value != value)
                    target.Value = value;
            }

            BindOneWay(source, nameof(Lib.ProgressBar.Minimum), apply);
            BindOneWay(source, nameof(Lib.ProgressBar.Maximum), apply);
            BindOneWay(source, nameof(Lib.ProgressBar.Value), apply);
        }

        public void BindComboBoxList<T>(Lib.ListView<T> source, ComboBox target, bool twoWaySelection = true)
        {
            void syncList()
            {
                target.BeginUpdate();
                try
                {
                    var selected = source.SelectedIndex;
                    target.Items.Clear();
                    foreach (var item in source.List)
                        target.Items.Add(item);
                    if (selected >= 0 && selected < target.Items.Count)
                        target.SelectedIndex = selected;
                    else
                        target.SelectedIndex = -1;
                }
                finally
                {
                    target.EndUpdate();
                }
            }

            void syncSelection()
            {
                var selected = source.SelectedIndex;
                if (selected >= 0 && selected < target.Items.Count)
                {
                    if (target.SelectedIndex != selected)
                        target.SelectedIndex = selected;
                }
                else if (target.SelectedIndex != -1)
                {
                    target.SelectedIndex = -1;
                }
            }

            syncList();

            ListChangedEventHandler listHandler = (s, e) => syncList();
            source.List.ListChanged += listHandler;
            _disposers.Add(() => source.List.ListChanged -= listHandler);

            BindOneWay(source, nameof(Lib.ListView<T>.SelectedIndex), syncSelection);
            BindEnabled(source, target);
            BindVisible(source, target);

            if (!twoWaySelection)
                return;

            EventHandler selectionHandler = (s, e) =>
            {
                if (source.SelectedIndex != target.SelectedIndex)
                    source.SelectedIndex = target.SelectedIndex;
            };
            target.SelectedIndexChanged += selectionHandler;
            _disposers.Add(() => target.SelectedIndexChanged -= selectionHandler);
        }

        public void BindListBoxItems<T>(
            Lib.ListView<T> source,
            ListBox target,
            Func<T, object> itemProjector,
            bool twoWaySelection = true)
        {
            void syncList()
            {
                target.BeginUpdate();
                try
                {
                    var selected = source.SelectedIndex;
                    target.Items.Clear();
                    foreach (var item in source.List)
                        target.Items.Add(itemProjector(item));
                    if (selected >= 0 && selected < target.Items.Count)
                        target.SelectedIndex = selected;
                    else
                        target.SelectedIndex = -1;
                }
                finally
                {
                    target.EndUpdate();
                }
            }

            void syncSelection()
            {
                var selected = source.SelectedIndex;
                if (selected >= 0 && selected < target.Items.Count)
                {
                    if (target.SelectedIndex != selected)
                        target.SelectedIndex = selected;
                }
                else if (target.SelectedIndex != -1)
                {
                    target.SelectedIndex = -1;
                }
            }

            syncList();

            ListChangedEventHandler listHandler = (s, e) => syncList();
            source.List.ListChanged += listHandler;
            _disposers.Add(() => source.List.ListChanged -= listHandler);

            BindOneWay(source, nameof(Lib.ListView<T>.SelectedIndex), syncSelection);
            BindEnabled(source, target);
            BindVisible(source, target);

            if (!twoWaySelection)
                return;

            EventHandler selectionHandler = (s, e) =>
            {
                if (source.SelectedIndex != target.SelectedIndex)
                    source.SelectedIndex = target.SelectedIndex;
            };
            target.SelectedIndexChanged += selectionHandler;
            _disposers.Add(() => target.SelectedIndexChanged -= selectionHandler);
        }

        public void BindOneWay(INotifyPropertyChanged source, string propertyName, Action apply)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (apply == null)
                throw new ArgumentNullException(nameof(apply));

            apply();

            PropertyChangedEventHandler handler = (s, e) =>
            {
                if (e.PropertyName == null || e.PropertyName == propertyName)
                    apply();
            };
            source.PropertyChanged += handler;
            _disposers.Add(() => source.PropertyChanged -= handler);
        }

        public void Track(Action dispose)
        {
            if (dispose == null)
                throw new ArgumentNullException(nameof(dispose));
            _disposers.Add(dispose);
        }

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;
            for (var i = _disposers.Count - 1; i >= 0; i--)
                _disposers[i]();
            _disposers.Clear();
        }
    }
}
