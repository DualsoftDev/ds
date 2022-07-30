using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Common
{
    public class ConcurrentHashSet<T> : ConcurrentDictionary<T, T>
    {
        public bool Add(T item) => TryAdd(item, item);
    }
}
