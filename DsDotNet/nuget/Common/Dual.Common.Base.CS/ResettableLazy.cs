using System;
using System.Threading;

namespace Dual.Common.Base.CS
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
}
