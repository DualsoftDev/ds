namespace Dual.Common.Base.FS

open System
open System.Runtime.CompilerServices

/// F# Option type 의 C# 확장 메소드
type FOption =
    /// F# Option type 의 Some 값 생성
    static member CreateSome(x:'t) = Some x
    /// F# Option type 의 None 값 생성
    static member CreateNone<'t>(): 't option = None

// F# 전용 Option extension 은 TypeExtension/Em.Option.fs 참고
type EmOption =
    /// Option 값이 Some 인지 검사
    [<Extension>] static member IsSome(x:'x option) = x |> Option.isSome
    /// Option 값이 None 인지 검사
    [<Extension>] static member IsNone(x:'x option) = x |> Option.isNone
    /// Option 에 map 적용 (C# function 적용)
    [<Extension>] static member Map(x:'x option, f:Func<'x, 'y>) = x |> Option.map (fun a -> f.Invoke(a))
    /// Option 에 iter 적용 (C# function 적용)
    [<Extension>] static member Iter(x:'x option, f:Action<'x>) = x |> Option.iter (fun a -> f.Invoke(a))
    /// Option 에 bind 적용 (C# function 적용)
    [<Extension>] static member Bind(x:'x option, f:Func<'x, 'y option>) = x |> Option.bind (fun a -> f.Invoke(a))

    /// Option 에 map 적용 (C# function 적용)
    [<Extension>] static member OptMap(x:'x option, f:Func<'x, 'y>) = x.Map(f)
    /// Option 에 iter 적용 (C# function 적용)
    [<Extension>] static member OptIter(x:'x option, f:Action<'x>) = x.Iter(f)
    /// Option 에 bind 적용 (C# function 적용)
    [<Extension>] static member OptBind(x:'x option, f:Func<'x, 'y option>) = x.Bind(f)

    /// System.Nullable -> Option
    [<Extension>] static member ToOption(x:Nullable<'a>) = x |> Option.ofNullable
    /// Option -> System.Nullable
    [<Extension>] static member ToNullable(x:'x option) = x |> Option.toNullable
    /// Option -> obj
    [<Extension>] static member ToObj(x:'x option) = x |> Option.toObj

    /// Option<string> 을 Some 이면 string None 이면 null 로 반환
    [<Obsolete("Use Option.toObj")>]
    [<Extension>] static member ToNullableString(x:string option) = x |> Option.defaultValue null

    /// Boolean 값이 true 일 때에만 f() 적용 Some 값 반환.
    [<Extension>] static member ToOptionWith(condition:bool, f:unit -> 'a) = if condition then Some (f()) else None

    /// Option 이 Some 이면 someFunc 적용한 값, None 이면 noneFunc 적용한 option 값 반환
    [<Extension>]
    static member MatchMap(x:'x option, someFunc:Func<'x, 'y>, noneFunc: Func<'y>) =
        match x with
        | Some s -> someFunc.Invoke(s)
        | None -> noneFunc.Invoke()

    /// Option 이 Some 이면 someAction 적용, None 이면 noneAction 적용
    [<Extension>]
    static member Match(x:'x option, someAction:Action<'x>, noneAction: Action) =
        match x with
        | Some s -> someAction.Invoke(s)
        | None -> noneAction.Invoke()

