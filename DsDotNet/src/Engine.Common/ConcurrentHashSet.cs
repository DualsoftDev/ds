using System.Collections.Concurrent;

namespace Engine.Common
{
    public class ConcurrentHashSet<T> : ConcurrentDictionary<T, T>
    {
        public bool Add(T item) => TryAdd(item, item);
    }
}
