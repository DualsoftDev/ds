#if MEMENTO
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

using Dual.Common.Core;

using Memento;


namespace Dual.Common.Utils.UndoRedo
{
    /// <summary>
    /// Undo/Redo 를 지원하는 collection
    /// </summary>
    public class UndoableCollection<T> : ObservableCollection<T>
        where T : INotifyPropertyChanged, INotifyPropertyChanging
    {
        Mementor _mementor;
        ObservableCollection<T> _cache = new();
        public UndoableCollection(Mementor m, IEnumerable<T> xs=null)
        {
            xs?.Iter(Add);   // 최초 추가분은 undo/redo 대상에서 제외
            _mementor = m;
            this.CollectionChanged += (s, e) =>
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Move:
                        m.ElementIndexChange(this, (T)e.OldItems[0], e.OldStartingIndex);
                        _cache = new ObservableCollection<T>(this);
                        break;

                    case NotifyCollectionChangedAction.Add:
                    case NotifyCollectionChangedAction.Replace:
                    case NotifyCollectionChangedAction.Remove:
                    case NotifyCollectionChangedAction.Reset:       // check 필요.. 추가된 case
                        if (e.OldItems != null)
                        {
                            foreach (var item in e.OldItems.Cast<T>())
                            {
                                m.ElementRemove(this, item, _cache.IndexOf(item));
                                item.PropertyChanging -= propertyChanging!;
                            }
                        }

                        if (e.NewItems != null)
                        {
                            foreach (var item in e.NewItems.Cast<T>())
                            {
                                m.ElementAdd(this, item);
                                item.PropertyChanging += propertyChanging!;
                            }
                        }

                        _cache = new ObservableCollection<T>(this);
                        break;

                    default:
                        throw new NotSupportedException();
                }

            };
        }
        void propertyChanging(object sender, PropertyChangingEventArgs args)
        {
            var t = sender.GetType();
            var propName = args.PropertyName;
            var member = t.GetMembers(BindingFlags.Public | BindingFlags.Instance)
                        .FirstOrDefault(x => x.MemberType == MemberTypes.Property && x.Name == propName)
                        ;

            var attr = member?.GetCustomAttribute<UndoRedoIgnoreAttribute>(true);
            if (attr != null)
                return;

            _mementor.PropertyChange(sender, propName);
        }
    }
}
#endif
