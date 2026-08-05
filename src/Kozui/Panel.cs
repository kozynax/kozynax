using System;
using System.Collections;
using System.Collections.Generic;

namespace ZXMAK2.Host.WinForms.Lib
{
    /// <summary>
    /// Kozui container that owns a child control tree for portable layout.
    /// </summary>
    public class Panel : KozuiControl
    {
        private readonly ChildCollection _children;

        public Panel()
        {
            _children = new ChildCollection(this);
        }

        public IList<KozuiControl> Children => _children;

        public void Add(KozuiControl child) => _children.Add(child);

        private sealed class ChildCollection : IList<KozuiControl>
        {
            private readonly Panel _owner;
            private readonly List<KozuiControl> _items = new List<KozuiControl>();

            public ChildCollection(Panel owner)
            {
                _owner = owner;
            }

            public KozuiControl this[int index]
            {
                get => _items[index];
                set
                {
                    Detach(_items[index]);
                    Attach(value);
                    _items[index] = value;
                }
            }

            public int Count => _items.Count;
            public bool IsReadOnly => false;

            public void Add(KozuiControl item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));
                Attach(item);
                _items.Add(item);
            }

            public void Clear()
            {
                foreach (var item in _items)
                    Detach(item);
                _items.Clear();
            }

            public bool Contains(KozuiControl item) => _items.Contains(item);
            public void CopyTo(KozuiControl[] array, int arrayIndex) => _items.CopyTo(array, arrayIndex);
            public IEnumerator<KozuiControl> GetEnumerator() => _items.GetEnumerator();
            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
            public int IndexOf(KozuiControl item) => _items.IndexOf(item);

            public void Insert(int index, KozuiControl item)
            {
                if (item == null)
                    throw new ArgumentNullException(nameof(item));
                Attach(item);
                _items.Insert(index, item);
            }

            public bool Remove(KozuiControl item)
            {
                if (!_items.Remove(item))
                    return false;
                Detach(item);
                return true;
            }

            public void RemoveAt(int index)
            {
                Detach(_items[index]);
                _items.RemoveAt(index);
            }

            private void Attach(KozuiControl child)
            {
                if (child.Parent is Panel previous && !ReferenceEquals(previous, _owner))
                    previous.Children.Remove(child);
                child.Parent = _owner;
            }

            private static void Detach(KozuiControl child)
            {
                if (child != null)
                    child.Parent = null;
            }
        }
    }
}
