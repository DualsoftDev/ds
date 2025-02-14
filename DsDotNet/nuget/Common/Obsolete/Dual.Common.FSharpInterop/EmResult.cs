using System;
using Microsoft.FSharp.Core;

namespace Dual.Common.FSharpInterop;

public static class EmResult
{
    /// <summary>
    /// F# Result type 에 대한 C# Match extension method
    /// </summary>
    public static TResult Match<TOk, TError, TResult>(this FSharpResult<TOk, TError> value, Func<TOk, TResult> okFunc, Func<TError, TResult> errorFunc) =>
        value.IsOk ? okFunc(value.ResultValue) : errorFunc(value.ErrorValue);

    /// <summary>
    /// F# Result type 에 대한 C# Map extension method
    /// </summary>
    public static FSharpResult<TResult, TError> Map<TOk, TError, TResult>(
        this FSharpResult<TOk, TError> result,
        Func<TOk, TResult> mapFunc)
    {
        return result.Match(
            okValue => FSharpResult<TResult, TError>.NewOk(mapFunc(okValue)),
            errorValue => FSharpResult<TResult, TError>.NewError(errorValue));
    }

    /// <summary>
    /// F# Result type 에 대한 C# Bind extension method
    /// </summary>
    public static FSharpResult<TResult, TError> Bind<TOk, TError, TResult>(
        this FSharpResult<TOk, TError> result,
        Func<TOk, FSharpResult<TResult, TError>> bindFunc)
    {
        return result.Match(
            okValue => bindFunc(okValue),
            errorValue => FSharpResult<TResult, TError>.NewError(errorValue));
    }
}