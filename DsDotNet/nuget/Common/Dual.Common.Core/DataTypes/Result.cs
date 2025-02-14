// https://dev.to/ephilips/better-error-handling-in-c-with-result-types-4aan

using Newtonsoft.Json.Linq;

using System;
using System.Threading.Tasks;

namespace Dual.Common.Core
{
    /// <summary>
    /// Result type (functional)
    /// </summary>
    /// <typeparam name="T">Success case</typeparam>
    /// <typeparam name="E">Error case</typeparam>
    [Obsolete("Use F# Result<T, E> directly.")]
    public readonly struct Result<T, E>
    {
        private readonly bool _success;
        /// <summary>
        /// Ok value
        /// </summary>
        public readonly T Value;
        /// <summary>
        /// Error value
        /// </summary>
        public readonly E Error;

        private Result(T v, E e, bool success)
        {
            Value = v;
            Error = e;
            _success = success;
        }

        public bool IsOk => _success;
        public bool IsError => !_success;

        public static Result<T, E> Ok(T v)
        {
            return new(v, default(E), true);
        }

        public static Result<T, E> Err(E e)
        {
            return new(default(T), e, false);
        }

        public static implicit operator Result<T, E>(T v) => new(v, default(E), true);
        public static implicit operator Result<T, E>(E e) => new(default(T), e, false);

        public R Match<R>(
                Func<T, R> success,
                Func<E, R> failure) =>
            _success ? success(Value) : failure(Error);
        public void Iter(
                Action<T> success,
                Action<E> failure)
        {
            if (_success)
                success(Value);
            else
                failure(Error);
        }

        public async Task IterAsync(
        Func<T, Task> success,
        Func<E, Task> failure)
        {
            if (_success)
                await success(Value);
            else
                await failure(Error);
        }

    }
}
