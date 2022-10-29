namespace Engine.Common.FS

open System
open System.Linq
open System.Collections.Generic
open System.Runtime.CompilerServices

[<AutoOpen>]
module NullableModule1 =
    let (|Null|Value|) (x: _ Nullable) =
        if x.HasValue then Value x.Value else Null

// http://www.fssnip.net/1A/title/Functions-around-nullable-types
// https://gist.github.com/nagat01/1263809
[<RequireQualifiedAccess>]
module Nullable =
    /// C# Nullable to F# Option : value type 에만 적용됨
    let toOption (n:Nullable<_>) =
       if n.HasValue
       then Some n.Value
       else None

    /// F# option to C# Nullable
    let ofOption (o:option<_>) =
        match o with
        | Some v -> Nullable<_>(v)
        | None -> Nullable<_>()

    let bind f x =
        match x with
        | Null -> Nullable()
        | Value v -> f v
    let create x = Nullable x
    let defaultValue def (x: _ Nullable) = if x.HasValue then x.Value else def
    let hasValue (x: _ Nullable) = x.HasValue
    let isNull (x: _ Nullable) = not x.HasValue
    let count (x: _ Nullable) = if x.HasValue then 1 else 0
    let forceGetValue (x: _ Nullable) = x.Value

    let fold f state x =
        match x with
        | Null -> state
        | Value v -> f state v
    let foldBack f x state =
        match x with
        | Null -> state
        | Value v -> f x state
    let exists p x =
        match x with
        | Null -> false
        | Value v -> p x
    let forall p x =
        match x with
        | Null -> true
        | Value v -> p x
    let iter f x =
        match x with
        | Null -> ()
        | Value v -> f v
    let map f x =
        match x with
        | Null -> Nullable()
        | Value v -> Nullable(f v)

    let toArray x =
        match x with
        | Null -> [||]
        | Value v -> [| v |]
    let toList x =
        match x with
        | Null -> []
        | Value v -> [v]


[<Extension>]
type NullExt =
    [<Extension>] static member NullableMap(x:'T when 'T: not struct, f) = if isNull x then null else f x
    [<Extension>] static member NullableMap(x, f) = Nullable.map f x
    [<Extension>] static member NullableIter(x:'T when 'T: not struct, f) = if isNull x then () else f x
    [<Extension>] static member NullableIter(x, f) = Nullable.iter f x
    [<Extension>] static member DefaultValue(x:'T when 'T: not struct, def) = if isNull x then def else x
    [<Extension>] static member DefaultValue(x, def) = Nullable.defaultValue def x
    [<Extension>]
    static member ToReference(x:'T option when 'T: not struct) =
        match x with
        | Some v -> v
        | None -> null




[<AutoOpen>]
module NullableModule =
    /// C# Nullable to F# Option
    let n2o nullable = Nullable.toOption nullable

    /// F# option to C# Nullable : value type 이 아닌 경우, compile error 발생
    let o2n optVal = Nullable.ofOption optVal

    type Nullable<'a when 'a : (new : unit -> 'a ) and 'a : struct and 'a :> ValueType> with
      member x.bind f =
        if x.HasValue then f x.Value |> Some else None

      member x.count =
        if x.HasValue then 1 else 0

      member x.exists f =
        x.HasValue && f x.Value

      member x.fold f a =
        if x.HasValue then f a x.Value else a

      member x.foldBack f  a =
        if x.HasValue then f x.Value a else a

      member x.forall f =
        not x.HasValue || f x.Value

      member x.iter f  =
        if x.HasValue then f x.Value

      member x.map f  =
        if x.HasValue then f x |> Some else None