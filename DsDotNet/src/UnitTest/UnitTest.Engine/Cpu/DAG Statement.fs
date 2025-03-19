namespace T.CPU

open NUnit.Framework

open Dual.Common.Core.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS


type Spec08_DAGStatement() =
    inherit EngineTestBaseClass()

    let t = CpuTestSample(WINDOWS)
    [<Test>]
    member __.``D1 DAG  Start`` () =
        for real in t.Reals do
            real.CoinStartActive() |> doChecks
            real.CoinStartPassive() |> doChecks

    [<Test>]
    member __.``D3 DAG Coin End InReal`` () =
        for real in t.Reals do
            real.CoinEndActive() |> doChecks
            real.CoinEndPassive() |> doChecks

    [<Test>]
    member __.``D4 DAG Coin Reset`` () =
        for real in t.Reals do
            real.D3_CoinReset() |> doChecks

