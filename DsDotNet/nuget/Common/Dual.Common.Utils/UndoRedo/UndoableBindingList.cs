#if MEMENTO
using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;


namespace Dual.Common.Utils.UndoRedo
{
    /// <summary>
    /// UndoableCollection<T> 를 BindingList<T> 와 연동하기 위한 class
    /// </summary>
    public class UndoableBindingList<T> : BindingList<T>
        where T : INotifyPropertyChanged, INotifyPropertyChanging
    {
        UndoableCollection<T> _undoableCollection;
        public UndoableBindingList(UndoableCollection<T> xs)
            : base(xs.ToList()) // .ToList() 를 통해 새로운 collection 을 생성해서 binding
        {
            _undoableCollection = xs;

            _undoableCollection.CollectionChanged += (s, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        e.NewItems.Cast<T>().Iter(Add);
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        e.OldItems.Cast<T>().Iter(x => Remove(x));
                        break;
                    case NotifyCollectionChangedAction.Reset:
                        Clear();
                        break;

                    default:
                        throw new NotSupportedException();
                }
            };
        }
    }
}
#endif
