// https://fsharpforfunandprofit.com/posts/computation-expressions-intro/

module Dual.Common.UntilSomeBuilder

type UntilSomeBuilder() =
    member __.ReturnFrom(x) = x
    member __.Combine (a,b) = 
        match a with
        | Some _ -> a  // a succeeds -- use it
        | None -> b    // a fails -- use b instead
    member __.Delay(f) = f()

/// <summary>
/// 성공할 때까지 계속 진행하는 workflow
/// cf. maybe 는 계속 진행하면서 실패하면 종료하는 workflow
/// </summary>
let untilSome = new UntilSomeBuilder()
let oneof = untilSome

