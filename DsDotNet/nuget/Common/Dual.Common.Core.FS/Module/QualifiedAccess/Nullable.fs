namespace Dual.Common.Core.FS

open System
open System.Linq
open System.Collections.Generic
open System.Runtime.CompilerServices


[<AutoOpen>]
module NullableModule1 =
    let (|Null|Value|) (x: _ Nullable) =
        if x.HasValue then Value x.Value else Null

/// System.Nullable<'T> 에 대한 확장
///
/// - System.Nullable<'T> 은 'T 가 value type 이어야만 한다.  reference type 이면 compile 오류 발생
///
/// - 가급적 Option<'T> 이용.
///
/// - 불가피한 경우
///
///   * ORM 에서 class 가 Null 을 가질 수 있는 경우.  (DB column mapping 시)
// http://www.fssnip.net/1A/title/Functions-around-nullable-types
// https://gist.github.com/nagat01/1263809
[<Obsolete("가급적 Option<'T> 이용")>]
[<RequireQualifiedAccess>]
module Nullable =
    /// System.Nullable 을 F# Option<> 으로 변환
    let toOption (n:Nullable<_>) =
       if n.HasValue
       then Some n.Value
       else None

    /// F# option<> 을 System.Nullable<> 로 변환
    let ofOption (o:option<_>) =
        match o with
        | Some v -> Nullable<_>(v)
        | None -> Nullable<_>()


    let ofValue<'t when 't : struct and 't : (new : unit -> 't) and 't :> ValueType> (o: 't) = Nullable<'t>(o)

    // 다음과 같은 함수는 정의할 수 없다.  obj (reference type) 에 대해서는 Nullable<> 을 만들 수 없다.
    // let ofObj<'t> o = ...


    let bind f x =
        match x with
        | Null -> Nullable()
        | Value v -> f v
    let create x = Nullable x
    let defaultValue def (x: _ Nullable) = if x.HasValue then x.Value else def
    let defaultValue2<'t when 't : null> (def: 't) (x: 't) =
        if isNull x then def else x
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

[<RequireQualifiedAccess>]
module Null =
    let defaultValue<'t when 't : null> (def: 't) (x: 't) =
        if isNull x then def else x
    let defaultWith<'t when 't : null> (gen: unit -> 't) (x: 't) =
        if isNull x then gen() else x


#nowarn "0044"  // warning FS0044: 이 구문은 사용되지 않습니다.. 가급적 Option<'T> 이용


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