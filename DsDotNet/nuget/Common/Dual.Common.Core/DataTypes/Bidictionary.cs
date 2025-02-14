using Dual.Common.Base.CS;

using System;
using System.Collections.Generic;

namespace Dual.Common.Core
{
    // https://stackoverflow.com/questions/10966331/two-way-bidirectional-dictionary-in-c/10966684
    /// <summary>
    /// 양방향 검색 dictionary
    /// </summary>
    public class Bidictionary<K, V>
    {
        private Dictionary<K, V> _forward = new Dictionary<K, V>();
        private Dictionary<V, K> _backward = new Dictionary<V, K>();

        public Bidictionary()
        {
            this.Forward = new Indexer<K, V>(_forward);
            this.Backward = new Indexer<V, K>(_backward);
        }

        public class Indexer<T3, T4>
        {
            private Dictionary<T3, T4> _dictionary;
            public Indexer(Dictionary<T3, T4> dictionary)
            {
                _dictionary = dictionary;
            }
            public T4 this[T3 index]
            {
                get { return _dictionary[index]; }
                set { _dictionary[index] = value; }
            }
        }

        public void Add(K t1, V t2)
        {
            _forward.Add(t1, t2);
            _backward.Add(t2, t1);
        }

        public void AddRange(IEnumerable<KeyValuePair<K, V>> range) =>
            range.Iter(kv => Add(kv.Key, kv.Value));
        public void AddRange(IEnumerable<Tuple<K, V>> range) =>
            range.Iter(tpl => Add(tpl.Item1, tpl.Item2));

        public Indexer<K, V> Forward { get; private set; }
        public Indexer<V, K> Backward { get; private set; }
    }
}
