using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Dual.Common.Core
{
    public class OptionSerializable<T>
    {
        public T _value { get; set; }
        public bool _hasValue { get; set; }
        public OptionSerializable()
        {
            _value = default(T);
            _hasValue = false;
        }

        private OptionSerializable(T value)
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
                {
                    // serialize 지원을 위해서 throw 를 완화..
                    // throw new InvalidOperationException("Option does not have a value.");
                    return default(T);
                    
                }
                return _value;
            }
        }

        public static OptionSerializable<T> Some(T value)
        {
            return new OptionSerializable<T>(value);
        }

        public static OptionSerializable<T> None()
        {
            return new OptionSerializable<T>();
        }

        public static implicit operator OptionSerializable<T>(T value)
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


        public OptionSerializable<R> Map<R>(Func<T, R> mapper)
        {
            if (_hasValue)
                return OptionSerializable<R>.Some(mapper(_value));
            else
                return OptionSerializable<R>.None();
        }

        public OptionSerializable<R> Bind<R>(Func<T, OptionSerializable<R>> binder)
        {
            if (_hasValue)
                return binder(_value);
            else
                return OptionSerializable<R>.None();
        }

        public static OptionSerializable<T> FromReference<U>(U reference) where U : class, T
        {
            if (reference == null)
                return OptionSerializable<T>.None();
            else
                return OptionSerializable<T>.Some(reference);
        }
    }

    public static class OptionSerializableExtensions
    {
        public static IEnumerable<R> CsChoose<T, R>(this IEnumerable<T> source, Func<T, OptionSerializable<R>> chooser)
        {
            foreach (var item in source)
            {
                var option = chooser(item);
                if (option.IsSome)
                {
                    yield return option.Value;
                }
            }
        }
        //public static IEnumerable<U> CsChoose<T, U>(this IEnumerable<OptionSerializable<T>> source, Func<T, OptionSerializable<U>> chooser)
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
        //            yield return OptionSerializable<U>.None();
        //    }
        //}
    }
}
