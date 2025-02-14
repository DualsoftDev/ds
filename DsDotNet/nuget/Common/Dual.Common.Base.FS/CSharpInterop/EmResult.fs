namespace Dual.Common.Base.FS

open System
open System.Runtime.CompilerServices

type EmResult =
    /// Result 값이 Ok 인지 검사
    [<Extension>] static member IsOk(x:Result<'T, 'E>) = x |> Result.isOk
    /// Result 값이 Error 인지 검사
    [<Extension>] static member IsError(x:Result<'T, 'E>) = x |> Result.isError

    /// Result 에서 Ok 값 가져오기 (Error일 경우 fail)
    //[<Obsolete("C# 에서는 Use Reslut<_>.ResultValue")>]
    [<Extension>]
    static member GetOkValue(x:Result<'T, 'E>) =
        match x with
        | Error e -> failwith "Not OK value"
        | Ok v -> v

    /// Result 에서 Error 값 가져오기 (Ok일 경우 fail)
    //[<Obsolete("C# 에서는 Use Reslut<_>.ErrorValue")>]
    [<Extension>]
    static member GetErrorValue(x:Result<'T, 'E>) =
        match x with
        | Error e -> e
        | Ok _ -> failwith "Not ERROR value"

    /// Result 에서 Ok 값 가져오기 (Error일 경우 기본값 반환)
    [<Extension>] static member DefaultValue(xr:Result<'T, 'E>, coverValue:'T) = Result.defaultValue coverValue xr
    [<Extension>] static member DefaultWith(xr:Result<'T, 'E>, f:'E->'T) = Result.defaultWith f xr
    [<Extension>] static member DefaultWith(xr:Result<'T, 'E>, f:Func<'E, 'T>) = Result.defaultWith (fun err -> f.Invoke(err)) xr

    /// Result 에 map 적용 (C# function 적용)
    [<Extension>] static member Map(x:Result<'T, 'E>, f:Func<'T, 'U>) = x |> Result.map (fun a -> f.Invoke(a))
    /// Result 에 mapError 적용 (C# function 적용)
    [<Extension>] static member MapError(x:Result<'T, 'E>, f:Func<'E, 'U>) = x |> Result.mapError (fun e -> f.Invoke(e))
    /// Result 에 bind 적용 (C# function 적용)
    [<Extension>] static member Bind(x:Result<'T, 'E>, f:Func<'T, Result<'U, 'E>>) = x |> Result.bind (fun a -> f.Invoke(a))
    /// Result 에 iter 적용 (C# function 적용)
    [<Extension>] static member Iter(x:Result<'T, 'E>, f:Action<'T>) = x |> Result.iter (fun a -> f.Invoke(a))

    /// Result 에 map 적용 (C# function 적용)
    [<Extension>] static member RMap(x:Result<'T, 'E>, f:Func<'T, 'U>) = x.Map(f)
    /// Result 에 iter 적용 (C# function 적용)
    [<Extension>] static member RIter(x:Result<'T, 'E>, f:Action<'T>) = x.Iter(f)
    /// Result 에 bind 적용 (C# function 적용)
    [<Extension>] static member RBind(x:Result<'T, 'E>, f:Func<'T, Result<'U, 'E>>) = x.Bind(f)

    /// Boolean 값이 true 일 때에만 f() 적용해 Ok 반환, false 일 때는 e() 적용해 Error 반환
    [<Extension>]
    static member ToResultWith(condition:bool, okFunc:unit -> 'T, errorFunc:unit -> 'E) =
        if condition then Ok (okFunc()) else Error (errorFunc())

    /// Result 가 Ok 이면 okFunc 적용, Error 이면 errorFunc 적용해 다른 Result 반환
    [<Extension>]
    static member MatchMap(x:Result<'T, 'E>, okFunc:Func<'T, 'U>, errorFunc:Func<'E, 'U>) =
        match x with
        | Ok t -> okFunc.Invoke(t)
        | Error e -> errorFunc.Invoke(e)

    /// Result 가 Ok 이면 okAction 적용, Error 이면 errorAction 적용
    [<Extension>]
    static member Match(x:Result<'T, 'E>, okAction:Action<'T>, errorAction:Action<'E>) =
        match x with
        | Ok t -> okAction.Invoke(t)
        | Error e -> errorAction.Invoke(e)

    [<Extension>]
    static member ChooseOk(xs: seq<Result<'T, 'E>>, predicate: 'T -> bool) : seq<'T> =
        xs
        |> Seq.choose (function
            | Ok value when predicate value -> Some value
            | _ -> None)

    [<Extension>] static member ChooseOk(xs: seq<Result<'T, 'E>>) : seq<'T> = xs.ChooseOk(fun (x:'T) -> true)

