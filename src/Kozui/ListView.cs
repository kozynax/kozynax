using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ZXMAK2.Host.WinForms.Lib
{
    public class ListView<T> : KozuiControl
    {
        public delegate void IndexChangeEventHandler(object sender, int index);
        public event IndexChangeEventHandler SelectedIndexChanged;

        public static readonly object _lockObject = new object();

        private int _selectedIndex = -1;
        public int SelectedIndex { 
            get
            {
                return _selectedIndex;
            }
            set
            {
                if (_selectedIndex != value)
                {
                    lock (_lockObject)
                    {
                        _selectedIndex = value;
                        SelectedIndexChanged?.Invoke(this, _selectedIndex);
                    }
                }
            }
        }
        public IList<T> List { get; } = new List<T>();
    }
}
