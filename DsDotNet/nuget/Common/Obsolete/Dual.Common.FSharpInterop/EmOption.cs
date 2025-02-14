using Microsoft.FSharp.Core;

using System;

namespace Dual.Common.FSharpInterop
{
    public static class EmOption
    {
        public static bool IsSome<T>(this FSharpOption<T> optionValue) => FSharpOption<T>.get_IsSome(optionValue);
        public static bool IsNone<T>(this FSharpOption<T> optionValue) => FSharpOption<T>.get_IsNone(optionValue);

        /// <summary>
        /// F# Option type 에 대한 C# Match extension method
        /// </summary>
        public static TResult Match<T, TResult>(this FSharpOption<T> value, Func<T, TResult> someFunc, Func<TResult> noneFunc) =>
            value.IsSome() ? someFunc(value.Value) : noneFunc();

        /// <summary>
        /// F# Option type 에 대한 C# Map extension method
        /// </summary>
        public static FSharpOption<TResult> Map<T, TResult>(this FSharpOption<T> option, Func<T, TResult> mapFunc)
        {
            return option.Match(
                someValue => FSharpOption<TResult>.Some(mapFunc(someValue)),
                () => FSharpOption<TResult>.None);
        }

        /// <summary>
        /// F# Option type 에 대한 C# Bind extension method
        /// </summary>
        public static FSharpOption<TResult> Bind<T, TResult>(this FSharpOption<T> option, Func<T, FSharpOption<TResult>> bindFunc)
        {
            return option.Match(
                someValue => bindFunc(someValue),
                () => FSharpOption<TResult>.None);
        }


        /// <summary>
        /// null 대입 가능한 T 의 F# Option<T> 를 T type 으로 변환
        /// </summary>
        public static T ToReference<T>(this FSharpOption<T> option) where T: class
        {
            return option.Match(
                someValue => someValue,
                () => null);
        }
    }
}
