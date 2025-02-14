using System;
using System.Collections.Concurrent;

// ObservableConcurrentQueue : https://github.com/YounesCheikh/ObservableConcurrentQueue

namespace Dual.Common.Core
{
    public sealed class ObservableConcurrentQueue<T> : ConcurrentQueue<T>
    {
        public Action Added;

        public new void Enqueue(T item)
        {
            base.Enqueue(item);
            Added?.Invoke();
        }
    }
}


