using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace Engine.Common;

// ObservableConcurrentQueue : https://github.com/YounesCheikh/ObservableConcurrentQueue

public enum NotifyConcurrentQueueChangedAction
{
    Enqueue,
    Dequeue,
    Peek,
    Empty
}
public class NotifyConcurrentQueueChangedEventArgs<T> : EventArgs
{
    public NotifyConcurrentQueueChangedEventArgs(NotifyConcurrentQueueChangedAction action, T changedItem)
    {
        this.Action = action;
        this.ChangedItem = changedItem;
    }

    public NotifyConcurrentQueueChangedEventArgs(NotifyConcurrentQueueChangedAction action)
    {
        this.Action = action;
    }

    public NotifyConcurrentQueueChangedAction Action { get; private set; }

    public T ChangedItem { get; private set; }
}

public delegate void ConcurrentQueueChangedEventHandler<T>(
    object sender,
    NotifyConcurrentQueueChangedEventArgs<T> args);

public sealed class ObservableConcurrentQueue<T> : ConcurrentQueue<T>, INotifyCollectionChanged
{
    public event ConcurrentQueueChangedEventHandler<T> ContentChanged;
    public event NotifyCollectionChangedEventHandler CollectionChanged;

    public new void Enqueue(T item)    { EnqueueItem(item); }

    public new bool TryDequeue(out T result)
    {
        return TryDequeueItem(out result);
    }

    public new bool TryPeek(out T result)
    {
        var retValue = base.TryPeek(out result);
        if (retValue)
        {
            // Raise item dequeued event
            this.OnContentChanged(
                new NotifyConcurrentQueueChangedEventArgs<T>(NotifyConcurrentQueueChangedAction.Peek, result));
        }

        return retValue;
    }

    private void OnContentChanged(NotifyConcurrentQueueChangedEventArgs<T> args)
    {
        this.ContentChanged?.Invoke(this, args);
        NotifyCollectionChangedAction? action =
            args.Action switch
            {
                NotifyConcurrentQueueChangedAction.Enqueue => NotifyCollectionChangedAction.Add,
                NotifyConcurrentQueueChangedAction.Dequeue => NotifyCollectionChangedAction.Remove,
                NotifyConcurrentQueueChangedAction.Empty => NotifyCollectionChangedAction.Reset,
                _ => null,
            };

        // Raise event only when action is defined (Add, Remove or Reset)
        if (action.HasValue)
        {
            this.CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(action.Value, args.Action != NotifyConcurrentQueueChangedAction.Empty
                ? new List<T> { args.ChangedItem }
                : null));
        }
    }

    private void EnqueueItem(T item)
    {
        base.Enqueue(item);

        // Raise event added event
        this.OnContentChanged(
            new NotifyConcurrentQueueChangedEventArgs<T>(NotifyConcurrentQueueChangedAction.Enqueue, item));
    }

    private bool TryDequeueItem(out T result)
    {
        if (!base.TryDequeue(out result))
            return false;

        // Raise item dequeued event
        this.OnContentChanged(
            new NotifyConcurrentQueueChangedEventArgs<T>(NotifyConcurrentQueueChangedAction.Dequeue, result));

        if (this.IsEmpty)
        {
            // Raise Queue empty event
            this.OnContentChanged(
                new NotifyConcurrentQueueChangedEventArgs<T>(NotifyConcurrentQueueChangedAction.Empty));
        }

        return true;
    }
}
