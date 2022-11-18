[<AutoOpen>]
module Engine.Common.FS.Prelude

open System
open System.Runtime.CompilerServices

let dispose (x:#IDisposable) = if x <> null then x.Dispose()
let toString x = x.ToString()

/// x.ToText() 을 반환
let inline show x = (^T : (member ToText : unit->string) x)
/// x.ToText() 을 반환
let inline toText x = (^T : (member ToText : unit->string) x)


/// x.Name 을 반환
let inline name x = ( ^T: (member Name:string) x )

/// x.Value 을 반환
let inline value x = (^T : (member Value : 'v) x)

/// x 가 'T type 을 상속받는지 확인
let isType<'T> (x: obj) = typedefof<'T>.IsAssignableFrom(x.GetType())
let isSuperType<'T> (x: obj) = x.GetType().IsAssignableFrom(typeof<'T>)

/// 강제 type 변환
let forceCast<'T> (x: obj) = box x :?> 'T


let verify x = if not x then failwith "ERROR"

// https://github.com/fsharp/fsharp/blob/cb6cb5c410f537c81cf26825657ef3bb29a7e952/src/fsharp/FSharp.Core/printf.fs#L1645
let failwithf format =
    Printf.ksprintf failwith format

let noop() = ()

// https://stackoverflow.com/questions/11696484/type-does-not-have-null-as-a-proper-value
/// [<AllowNullLiteral>] 을 사용할 수 없는 객체에 대한 강제 null check.  use sparingly
let inline isItNull (x:'T when 'T : not struct) = obj.ReferenceEquals (x, null)

/// [<AllowNullLiteral>] 을 사용할 수 없는 F# class 에 대한 null instance 강제 생성.  use sparingly
let getNull<'T when 'T : not struct>():'T = Operators.Unchecked.defaultof<'T>

let inline nonNullSelector (nonNullValue:^T when ^T : not struct) (x:^T when ^T : not struct) =
    if isItNull x then nonNullValue else x

open System.Linq
let isInUnitTest() =
    AppDomain.CurrentDomain.GetAssemblies()
        .Select(fun a -> a.FullName)
        .Any(fun n -> n.StartsWith("Microsoft.VisualStudio.TestPlatform."))
        ;


[<Extension>]
type PreludeExt =
    [<Extension>] static member IsNull(x) = isItNull x
    [<Extension>] static member IsNonNull(x) = not <| isItNull x
    [<Extension>] static member NonNullSelector(nonNullValue, x) = nonNullSelector nonNullValue x
