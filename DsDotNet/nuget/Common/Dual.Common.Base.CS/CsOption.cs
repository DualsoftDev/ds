using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dual.Common.Base.CS
{
    [Obsolete("Use F# option directly.")]
    public struct CsOption<T>
    {
        private readonly T _value;
        private readonly bool _hasValue;

        private CsOption(T value)
        {
            _value = value;
            _hasValue = true;
        }

        public bool HasValue => _hasValue;
        public bool IsSome => _hasValue;
        public bool IsNone => !_hasValue;

        public T Value
        {
            get
            {
                if (!_hasValue)
                    throw new InvalidOperationException("Option does not have a value.");
                return _value;
            }
        }

        public static CsOption<T> Some(T value)
        {
            return new CsOption<T>(value);
        }

        public static CsOption<T> None()
        {
            return new CsOption<T>();
        }

        public static implicit operator CsOption<T>(T value)
        {
            return Some(value);
        }

        public void Iter(
                Action<T> onSome,
                Action onFailure)
        {
            if (_hasValue)
                onSome(Value);
            else
                onFailure();
        }

        public async Task IterAsync(
            Func<T, Task> onSome,
            Func<Task> onFailure)
        {
            if (_hasValue)
                await onSome(Value);
            else
                await onFailure();
        }

        public CsOption<TResult> Map<TResult>(Func<T, TResult> mapper)
        {
            if (_hasValue)
                return CsOption<TResult>.Some(mapper(_value));
            else
                return CsOption<TResult>.None();
        }

        public CsOption<TResult> Bind<TResult>(Func<T, CsOption<TResult>> binder)
        {
            if (_hasValue)
                return binder(_value);
            else
                return CsOption<TResult>.None();
        }

        public static CsOption<T> FromReference<U>(U reference) where U : class, T
        {
            if (reference == null)
                return CsOption<T>.None();
            else
                return CsOption<T>.Some(reference);
        }
    }

    public static class CsOptionExtensions
    {
        //public static IEnumerable<U> CsChoose<T, U>(this IEnumerable<T> source, Func<T, CsOption<U>> chooser)
        //{
        //    foreach (var item in source)
        //    {
        //        var option = chooser(item);
        //        if (option.IsSome)
        //        {
        //            yield return option.Value;
        //        }
        //    }
        //}
        //public static IEnumerable<U> CsChoose<T, U>(this IEnumerable<CsOption<T>> source, Func<T, CsOption<U>> chooser)
        //{
        //    foreach (var option in source)
        //    {
        //        if (option.IsSome)
        //        {
        //            var value = option.Value;
        //            var result = chooser(value);
        //            yield return result;
        //        }
        //        else
        //            yield return CsOption<U>.None();
        //    }
        //}
    }
}
