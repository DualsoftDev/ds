namespace Dual.Common.FS.LSIS
open System

[<AutoOpen>]
module GeneralExt =
    type System.Boolean with
        member b.Bind f = if b then f() else false

[<RequireQualifiedAccess>]
module Boolean =
    /// b 가 true 이면 f() 수행 결과 값.  b 가 false 이면 false
    let bind f b = if b then f() else false

module TestMe =
    let t() = printfn "True"; true
    let f() = printfn "False"; false

    let a1 = t().Bind(t)
    let a2 = t().Bind(f)

    let a3 = t() |> Boolean.bind(t)
    let a4 = t() |> Boolean.bind(f)


    let a5 = f().Bind(t)
    let a6 = f().Bind(f)

    let a7 = f() |> Boolean.bind(t)
    let a8 = f() |> Boolean.bind(f)
