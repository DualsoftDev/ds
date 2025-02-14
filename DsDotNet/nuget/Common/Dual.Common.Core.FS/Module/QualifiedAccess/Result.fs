namespace Dual.Common.Core.FS

open System.Runtime.CompilerServices
open System


[<RequireQualifiedAccess>]
module Result =
    let orElse y x =
        match x with
        | Ok _ -> x
        | _ -> y
    let orElseWith f x =
        match x with
        | Ok _ -> x
        | _ -> f()

    //let isOk     = function | Ok _ -> true   | _ -> false
    //let isError  = function | Ok _ -> false  | _ -> true

    let toOption = function | Ok v -> Some v | _ -> None
    let toList   = function | Ok v -> [v]    | _ -> []

    /// Ok f() 값.  수행 중 exception 발생하면 Error <Exception> 값
    let tryMap (f:unit -> 'T) : Result<'T, Exception> = try Ok <| f() with ex -> Error ex
    /// Ok f() 값.  수행 중 exception 발생하면 Error <Exception> 값
    let tryBind (f:unit -> Result<'T, Exception>) : Result<'T, Exception> = try f() with ex -> Error ex

    let equal r1 r2 =
        match r1, r2 with
        | Ok v1, Ok v2 -> v1 = v2
        | Error e1, Error e2 -> e1 = e2
        | _ -> false


    let filter (predicate: 'T -> bool) (error: 'E) (result: Result<'T, 'E>) : Result<'T, 'E> =
        match result with
        | Ok value when predicate value -> Ok value
        | Ok _ -> Error error
        | Error e -> Error e

    let flatten (result: Result<Result<'T, 'E>, 'E>) : Result<'T, 'E> =
        match result with
        | Ok (Ok value) -> Ok value
        | Ok (Error e) -> Error e
        | Error e -> Error e

    let toNullable (result: Result<'T, 'E>) : System.Nullable<'T> =
        match result with
        | Ok value -> System.Nullable(value)
        | Error _ -> System.Nullable()
    let toObj (result: Result<'T, 'E>) : obj =
        match result with
        | Ok value -> box value
        | Error _ -> null

    let ofOption (error: 'E) (opt: Option<'T>) : Result<'T, 'E> =
        match opt with
        | Some value -> Ok value
        | None -> Error error

    let ofNullable (value: System.Nullable<'T>) (error: 'E) : Result<'T, 'E> =
        if value.HasValue then
            Ok value.Value
        else
            Error error
    let ofObj (value: obj) (error: 'E) : Result<'T, 'E> =
        match value with
        | null -> Error error
        | _ -> Ok (value :?> 'T)

    let get (result: Result<'T, 'E>) : 'T =
        match result with
        | Ok value -> value
        | Error err -> failwith $"Error: {err}"

    let chooseOk (xs: seq<Result<'T, 'E>>, predicate: 'T -> bool) : seq<'T> =
        xs
        |> Seq.choose (function
            | Ok value when predicate value -> Some value
            | _ -> None)
