namespace Dual.Common.Core.FS

open System.Runtime.CompilerServices
open System


// C# 전용 Option extension 과 병합 필요: Dual.Common.Base.FS/EmOption.fs 참고
type OptionExt =
    /// Option.get
    [<Extension>] static member GetValue<'T>(xo:'T option) = Option.get xo
    [<Extension>] static member Tap<'T>(xo:'T option, f) = Option.iter f xo; xo

    /// Option.bind.  (>>=)
    [<Extension>] static member Bind(xo:'T option, f)     = Option.bind f xo
    /// Option.contains
    [<Extension>] static member Contains(xo:'T option, v) = Option.contains v xo
    /// Option.count
    [<Extension>] static member Count(xo:'T option)       = Option.count xo
    /// Option.exists
    [<Extension>] static member Exists(xo:'T option, f)   = Option.exists f xo
    /// Option.filter
    [<Extension>] static member Filter(xo:'T option, f)   = Option.filter f xo
    /// Option.flatten
    [<Extension>] static member Flatten(xoo)   = Option.flatten xoo
    /// Option.defaultValue.  '|?' 와 동일
    [<Extension>] static member DefaultValue(xo:'T option, defaultValue) = Option.defaultValue defaultValue xo
    /// Option.defaultWith
    [<Extension>] static member DefaultWith(xo:'T option, f) = Option.defaultWith f xo
    /// Option.iter.  (>>:)
    [<Extension>] static member Iter(xo:'T option, f)     = Option.iter f xo
    /// Option.map.  (>>-)
    [<Extension>] static member Map(xo:'T option, f)      = Option.map f xo
    /// Some 이면서 값이 value 이어야 true
    [<Extension>] static member IsSomeValue(xo:'T option, value) = match xo with | Some x -> x = value | None -> false
    /// None 값이거나, Some 인 경우 f 를 만족해야 true
    [<Extension>] static member IsNoneOrWith(xo:'T option, f) = match xo with | None -> true | Some x -> f x
    /// None 값이거나, Some 인 경우 coverValue 이어야 true
    [<Extension>] static member IsNoneOr(xo:'T option, coverValue) = match xo with | None -> true | Some x -> x = coverValue
    /// Option.orElse
    [<Extension>] static member OrElse(xo:'T option, coverValue) = Option.orElse coverValue xo    // <|>
    /// Option.orElseWith
    [<Extension>] static member OrElseWith(xo:'T option, f) = Option.orElseWith f xo
    /// Option.toArray
    [<Extension>] static member ToArray(xo:'T option)     = Option.toArray xo
    /// Option.toList
    [<Extension>] static member ToList(xo:'T option)      = Option.toList xo
    /// Option.toNullable
    [<Extension>] static member ToNullable(xo:'T option)  = Option.toNullable xo
    /// Option.toObj
    [<Extension>] static member ToObj(xo:'T option)       = Option.toObj xo
    /// Option.ofNullable
    [<Extension>] static member ToOption(v)     = Option.ofNullable v
    /// Option.ofObj
    [<Extension>] static member ToOption(obj)   = Option.ofObj obj

[<AutoOpen>]
module OptionExtModule =
    type Option<'F> with
        /// Option type 캐스팅
        ///
        /// - obj option cast
        ///
        /// - value type option cast: e.g int option -> double option
        ///
        /// - Some 이고, casting 이 안되면 exception.  see TryCast
        ///
        /// !!! Static 함수 Option.cast 는 존재하지 않음 !!! 현재까지 구현 불가능
        member x.Cast<'T>() : 'T option =
            match x with
            | None -> None
            | Some o when ((box o) :? 'T) -> Some ((box o) :?> 'T)
            | Some o when isType<'T> o -> Some (forceCast<'T> o)
            | Some o when typeof<'F>.IsValueType && typeof<'T>.IsValueType ->
                let converted = System.Convert.ChangeType(o, typeof<'T>)
                Some (converted :?> 'T)
            | _ -> None


[<RequireQualifiedAccess>]
module Bool =
    /// true 일 때에만 f() 를 수행한다.
    let iter f b = if b then f()

[<Extension>] // type OptionExt =
type BooleanExt =
    /// true 일 때에만 f() 를 수행한다.
    [<Extension>] static member Iter(x, f) = if x then f()
