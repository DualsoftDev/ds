namespace Dual.Common.Core.FS

open System.Runtime.CompilerServices
open System


// xr:Result<'T, 'E> 생략하면 다른 extension 과 헷갈려서 컴파일 에러 발생하니 주의
type ResultExt =
    [<Extension>] static member GetValue<'T>(xr:Result<'T, _>) = Result.get xr
    [<Extension>] static member Tap<'T>(xr:Result<'T, _>, f) = Result.iter f xr; xr

    /// Result.bind.  (>>=)
    [<Extension>] static member Bind(xr:Result<'T, 'E>, f)     = Result.bind f xr
    /// Result.contains
    [<Extension>] static member Contains(xr:Result<'T, 'E>, v) = Result.contains v xr
    /// Result.count
    [<Extension>] static member Count(xr:Result<'T, 'E>)       = Result.count xr
    /// Result.exists
    [<Extension>] static member Exists(xr:Result<'T, 'E>, f)   = Result.exists f xr
    /// Result.filter
    [<Extension>] static member Filter(xr:Result<'T, 'E>, f)   = Result.filter f xr
    /// Result.flatten
    [<Extension>] static member Flatten(xrr:Result<Result<'T, 'E>, 'E>) = Result.flatten xrr
    /// Result.defaultValue.  '|?' 와 동일
    [<Extension>] static member DefaultValue(xr:Result<'T, 'E>, defaultValue) = Result.defaultValue defaultValue xr
    /// Result.defaultWith
    [<Extension>] static member DefaultWith(xr:Result<'T, 'E>, f) = Result.defaultWith f xr
    /// Result.iter.  (>>:)
    [<Extension>] static member Iter(xr:Result<'T, 'E>, f)     = Result.iter f xr
    /// Result.map.  (>>-)
    [<Extension>] static member Map(xr:Result<'T, 'E>, f)      = Result.map f xr
    /// Some 이면서 값이 value 이어야 true
    [<Extension>] static member IsOkValue(xr:Result<'T, 'E>, value) = match xr with | Ok x -> x = value | Error _ -> false
    /// None 값이거나, Some 인 경우 f 를 만족해야 true
    [<Extension>] static member IsErrorOrWith(xr:Result<'T, 'E>, f) = match xr with | Error _ -> true | Ok x -> f x
    /// None 값이거나, Some 인 경우 coverValue 이어야 true
    [<Extension>] static member IsErrorOr(xr:Result<'T, 'E>, coverValue) = match xr with | Error _ -> true | Ok x -> x = coverValue
    /// Result.orElse
    [<Extension>] static member OrElse(xr:Result<'T, 'E>, coverValue) = Result.orElse coverValue xr    // <|>
    /// Result.orElseWith
    [<Extension>] static member OrElseWith(xr:Result<'T, 'E>, f) = Result.orElseWith f xr
    /// Result.toArray
    [<Extension>] static member ToArray(xr:Result<'T, 'E>)     = Result.toArray xr
    /// Result.toList
    [<Extension>] static member ToList(xr:Result<'T, 'E>)      = Result.toList xr
    /// Result.toNullable
    [<Extension>] static member ToNullable(xr:Result<'T, 'E>)  = Result.toNullable xr
    /// Result.toObj
    [<Extension>] static member ToObj(xr:Result<'T, 'E>)       = Result.toObj xr
    /// Result.ofNullable
    [<Extension>] static member ToResult(v)     = Result.ofNullable v
    /// Result.ofObj
    [<Extension>] static member ToResult(obj)   = Result.ofObj obj

    /// Match function for Result type.  C# 에서 쉽게 사용.
    [<Extension>]
    static member Match(x:Result<'T, 'Err>, okFunc, errFun) =
        match x with
        | Ok v -> okFunc v
        | Error e -> errFun e


[<AutoOpen>]
module ResultExtModule =
    type Result<'T, 'E> with
        member private x.helper<'T, 'E, 'U>(failOnFailure: bool) : Result<'U, 'E> =
            match x with
            | Error e -> Error e
            | Ok o when ((box o) :? 'U) -> Ok ((box o) :?> 'U)
            | Ok o when isType<'U> o -> Ok (forceCast<'U> o)
            | Ok o when typeof<'T>.IsValueType && typeof<'U>.IsValueType ->
                let converted = System.Convert.ChangeType(o, typeof<'U>)
                Ok (converted :?> 'U)
            | _ when failOnFailure -> failwith "Casting ERROR"
            | _ -> Error (box "Casting failure" :?> 'E)

        /// Result type 캐스팅
        ///
        /// - obj result cast
        ///
        /// - value type result cast: e.g Ok<int> -> Ok<double>
        ///
        /// - Ok 이고, casting 이 안되면 exception 발생
        member x.Cast<'U>() : Result<'U, 'E> = x.helper<'T, 'E, 'U>(true)

        /// Result type 캐스팅
        ///
        /// - obj result cast
        ///
        /// - value type result cast: e.g Ok<int> -> Ok<double>
        ///
        /// - Ok 이고, casting 이 안되면 Error 반환
        member x.TryCast<'U>() : Result<'U, 'E> = x.helper<'T, 'E, 'U>(false)

