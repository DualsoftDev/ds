using System;
using System.Collections.Concurrent;

namespace Engine.Common;

// ObservableConcurrentQueue : https://github.com/YounesCheikh/ObservableConcurrentQueue

public sealed class ObservableConcurrentQueue<T> : ConcurrentQueue<T>
{
    public Action Added;

    public new void Enqueue(T item)
    {
        base.Enqueue(item);
        Added?.Invoke();
    }
}
