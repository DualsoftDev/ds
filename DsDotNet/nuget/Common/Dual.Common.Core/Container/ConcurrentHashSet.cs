using System.Collections.Concurrent;

namespace Dual.Common.Core
{
    public class ConcurrentHashSet<T> : ConcurrentDictionary<T, T>
    {
        public bool Add(T item) => TryAdd(item, item);
    }
}
