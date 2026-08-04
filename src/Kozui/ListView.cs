using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace ZXMAK2.Host.WinForms.Lib
{
    public class ListView<T> : KozuiControl
    {
        public delegate void IndexChangeEventHandler(object sender, int index);

        public event IndexChangeEventHandler SelectedIndexChanged;

        private int _selectedIndex = -1;

        public int SelectedIndex
        {
            get => _selectedIndex;
            set
            {
                if (!SetProperty(ref _selectedIndex, value))
                    return;
                SelectedIndexChanged?.Invoke(this, _selectedIndex);
            }
        }

        public BindingList<T> List { get; } = new BindingList<T>();

        public void Reset(IEnumerable<T> items)
        {
            List.RaiseListChangedEvents = false;
            try
            {
                List.Clear();
                foreach (var item in items)
                    List.Add(item);
            }
            finally
            {
                List.RaiseListChangedEvents = true;
                List.ResetBindings();
            }
        }

        public void Reset(params T[] items)
            => Reset((IEnumerable<T>)items);

        public bool SequenceEqual(IEnumerable<T> other)
            => List.SequenceEqual(other);
    }
}
