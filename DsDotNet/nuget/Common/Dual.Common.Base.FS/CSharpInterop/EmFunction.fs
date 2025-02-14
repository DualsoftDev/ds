namespace Dual.Common.Base.FS

open System
open System.Runtime.CompilerServices

type DcTimer =
    /// action 수행시의 소요 시간 (ms) 반환
    [<Extension>] static member Duration(f: Action) = duration (fun () -> f.Invoke()) |> snd

    /// 인자가 없는 function 수행 후, (결과값, 소요 시간 (ms)) tuple 반환
    [<Extension>] static member Duration(f: Func<'r>) = duration (fun () -> f.Invoke())


type DcFunction =
    static member Noop() = ()
    static member Idf(a) = a

/// Function Extension Methods for C#
type EmFunction =

    /// 두 함수를 합성하는 함수 (C#에서 호출 가능)
    [<Extension>]
    static member Compose<'a, 'b, 'c> (f1: Func<'a, 'b>, f2: Func<'b, 'c>) : Func<'a, 'c> =
        Func<'a, 'c>(fun a -> f2.Invoke(f1.Invoke(a)))

    /// 함수 f 를 n 번 반복하는 함수 반환
    [<Extension>]
    static member ComposeNTimes<'a>(f: Func<'a, 'a>, n: int) : Func<'a, 'a> =
        let repeatedFunc = ntimes n (fun x -> f.Invoke(x))
        Func<'a, 'a>(fun x -> repeatedFunc x)

    /// C#에서 사용할 수 있는 'tee' 함수
    [<Extension>]
    static member Tee<'x>(x: 'x, f: Action<'x>) : 'x = tee (fun x -> f.Invoke(x)) x

    /// 시퀀스의 각 요소에 함수를 적용한 후, 원래 시퀀스를 반환. C# 호출 용
    [<Extension>]
    static member TeeForEach<'x>(xs: seq<'x>, f: Action<'x>) : seq<'x> = teeForEach (fun x0 -> f.Invoke(x0)) xs

    /// C#에서 사용할 수 있는 'memoize' 함수
    [<Extension>]
    static member Memoize<'a, 'b when 'a : equality>(f: Func<'a, 'b>) : Func<'a, 'b> =
        let memoized = memoize (fun x -> f.Invoke(x))
        Func<'a, 'b>(fun x -> memoized x)

    /// 현재 함수 이름 반환
    [<Extension>]
    static member GetFunctionName() =
        let stackTrace = new System.Diagnostics.StackTrace()
        let stackFrame = stackTrace.GetFrame(1)
        let methodBase = stackFrame.GetMethod()
        methodBase.Name

    /// Call stack function names
    [<Extension>]
    static member GetCallStackFunctionNames() =
        let stackTrace = new System.Diagnostics.StackTrace()
        stackTrace.GetFrames() |> Seq.map (fun f -> f.GetMethod().Name)
