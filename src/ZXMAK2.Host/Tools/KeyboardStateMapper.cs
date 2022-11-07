using System;
using System.Xml;
using System.Collections.Generic;


namespace ZXMAK2.Host.Entities.Tools
{
    public class KeyboardStateMapper<T>
        where T : struct
    {
        private readonly Dictionary<Key, T> m_map;

        public IEnumerable<Key> Keys { get { return m_map.Keys; } }

        public KeyboardStateMapper(Dictionary<Key, T> mapping)
        {
            m_map = mapping;
        }

        public T this[Key key]
        {
            get { return m_map[key]; }
        }
    }
}
