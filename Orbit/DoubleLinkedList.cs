using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orbit
{

    class DoubleKeyDictionary<Tkey, Tval> : IEnumerable<KeyValuePair<Tkey, Dictionary<Tkey, Tval>>>
    {
        readonly Dictionary<Tkey, Dictionary<Tkey, Tval>> data = new Dictionary<Tkey, Dictionary<Tkey, Tval>>();

        public void Add(Tkey key1, Tkey key2, Tval value)
        {
            // Add two links to the dictionary
            if (data.TryGetValue(key1, out Dictionary<Tkey, Tval> dict))
                dict[key2] = value;
            else
                data.Add(key1, new Dictionary<Tkey, Tval>() { { key2, value } });

            if (data.TryGetValue(key2, out dict))
                dict[key1] = value;
            else
                data.Add(key2, new Dictionary<Tkey, Tval>() { { key1, value } });
        }

        public Tval this[Tkey key1, Tkey key2]
        {
            get { return data[key1][key2]; }
            set { data[key1][key2] = value; }
        }

        public bool Contains(Tkey key1, Tkey key2)
        {
            if (data.TryGetValue(key1, out Dictionary<Tkey, Tval> data2))
                return data2.ContainsKey(key2);
            return false;
        }

        public bool TryGetValue(Tkey key1, Tkey key2, out Tval val)
        {
            if (data.TryGetValue(key1, out Dictionary<Tkey, Tval> data2))
                return data2.TryGetValue(key2, out val);

            val = default;
            return false;
        }

        public IEnumerator<KeyValuePair<Tkey, Dictionary<Tkey, Tval>>> GetEnumerator()
        {
            return data.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return data.GetEnumerator();
        }
    }
}
