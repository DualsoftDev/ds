using Dual.Common.Base.CS;

using System;
using System.Collections.Generic;

namespace Dual.Common.Core
{
    /// <summary>
    /// http://d.hatena.ne.jp/s-kita/20081129/1227957618
    /// </summary>
    /// <typeparam name="Key"></typeparam>
    /// <typeparam name="Value"></typeparam>
    public class Multimap<Key, Value>
    {
        private Dictionary<Key, List<Value>> d = new();

        public List<Value> this[Key k] => d.TryGetValue(k, out var value) ? value : null;

        public void Add(Key k, Value v)
        {
            if (d.ContainsKey(k))
            {
                d[k].Add(v);
            }
            else
            {
                List<Value> l = new List<Value>();
                l.Add(v);
                d.Add(k, l);
            }
        }

        public bool Remove(Key k)
        {
            if (d.ContainsKey(k))
                return d.Remove(k);

            return false;
        }

        public bool Remove(Key k, Value v)
        {
            if (d.ContainsKey(k))
            {
                d[k].Remove(v);
                if (d[k].IsNullOrEmpty())
                    d.Remove(k);

                return true;
            }

            return false;
        }

        public bool ContainsKey(Key k) { return d.ContainsKey(k); }
        public IEnumerator<KeyValuePair<Key, List<Value>>> GetEnumerator()
        {
            foreach (KeyValuePair<Key, List<Value>> pair in d)
            {
                yield return pair;
            }
        }

        public Dictionary<Key, List<Value>>.KeyCollection Keys { get { return d.Keys; } }
        public Dictionary<Key, List<Value>>.ValueCollection Values { get { return d.Values; } }
        public void Clear() { d.Clear(); }
        public Int32 Count { get { return d.Count; } }
    }

}
