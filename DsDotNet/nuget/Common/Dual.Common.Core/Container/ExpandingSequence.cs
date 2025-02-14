using System;
using System.Collections.Generic;

namespace Dual.Common.Core
{
    /// Functional programming in Python.pdf
    public class ExpandingSequence<T>
    {
        private List<T> _cache = new List<T>();
        private IEnumerator<T> _it;
        public ExpandingSequence(IEnumerable<T> seq)
        {
            _it = seq.GetEnumerator();
        }

        public T this[int index]
        {
            get
            {
                while (_cache.Count <= index)
                {
                    if (_it.MoveNext())
                        _cache.Add(_it.Current);
                    else
                        throw new IndexOutOfRangeException();
                }

                return _cache[index];
            }
        }

        public int Length => _cache.Count;
    }
}
