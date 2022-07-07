using System;
using System.Threading;
using System.Threading.Tasks;

namespace Dual.Common.Core.DataTypes
{
    // https://codereview.stackexchange.com/questions/183109/resettablelazyt-a-resettable-version-of-net-lazyt
    public class ResettableLazy<T>
    {
        private Lazy<T> _lazy;

        public bool IsValueCreated => _lazy.IsValueCreated;

        public T Value => _lazy.Value;

        public LazyThreadSafetyMode LazyThreadSafetyMode { get; }

        private readonly Func<T> _valueFactory;

        public ResettableLazy(Func<T> valueFactory
            , LazyThreadSafetyMode lazyThreadSafetyMode = LazyThreadSafetyMode.ExecutionAndPublication)
        {
            _valueFactory = valueFactory;
            LazyThreadSafetyMode = lazyThreadSafetyMode;
            _lazy = new Lazy<T>(_valueFactory, LazyThreadSafetyMode);
        }

        public ResettableLazy(Func<T> valueFactory, bool isThreadSafe)
            : this(valueFactory
                  , isThreadSafe
                      ? LazyThreadSafetyMode.ExecutionAndPublication
                      : LazyThreadSafetyMode.None)
        {
        }

        public void Reset()
        {
            _lazy = new Lazy<T>(_valueFactory, LazyThreadSafetyMode);
        }

        public void SetValue(T value)
        {
            _lazy = new Lazy<T>(() => value);
        }
    }


    public class ResettableAsyncLazy<T>
    {
        private AsyncLazy<T> _lazy;

        public bool IsValueCreated => _lazy.IsValueCreated;

        public T Value => _lazy.Value.Result;
        public Task<T> AsyncValue => _lazy.Value;

        public LazyThreadSafetyMode LazyThreadSafetyMode { get; }

        private readonly Func<T> _valueFactory;
        private readonly Func<Task<T>> _taskFactory;

        public ResettableAsyncLazy(Func<T> valueFactory
            , LazyThreadSafetyMode lazyThreadSafetyMode = LazyThreadSafetyMode.ExecutionAndPublication)
        {
            _valueFactory = valueFactory;
            LazyThreadSafetyMode = lazyThreadSafetyMode;
            _lazy = new AsyncLazy<T>(_valueFactory, LazyThreadSafetyMode);
        }

        public ResettableAsyncLazy(Func<T> valueFactory, bool isThreadSafe)
            : this(valueFactory
                  , isThreadSafe
                      ? LazyThreadSafetyMode.ExecutionAndPublication
                      : LazyThreadSafetyMode.None)
        {
        }

        public ResettableAsyncLazy(Func<Task<T>> taskFactory
            , LazyThreadSafetyMode lazyThreadSafetyMode = LazyThreadSafetyMode.ExecutionAndPublication)
        {
            _taskFactory = taskFactory;
            LazyThreadSafetyMode = lazyThreadSafetyMode;
            _lazy = new AsyncLazy<T>(_taskFactory, LazyThreadSafetyMode);
        }

        public ResettableAsyncLazy(Func<Task<T>> taskFactory, bool isThreadSafe)
            : this(taskFactory
                  , isThreadSafe
                      ? LazyThreadSafetyMode.ExecutionAndPublication
                      : LazyThreadSafetyMode.None)
        {
        }



        public void Reset()
        {
            if (_valueFactory != null)
                _lazy = new AsyncLazy<T>(_valueFactory, LazyThreadSafetyMode);
            else
                _lazy = new AsyncLazy<T>(_taskFactory, LazyThreadSafetyMode);
        }

        public void SetValue(T value)
        {
            _lazy = new AsyncLazy<T>(() => value);
        }
    }
}
