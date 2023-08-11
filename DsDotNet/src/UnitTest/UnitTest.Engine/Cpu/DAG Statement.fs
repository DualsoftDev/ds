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

    let t = CpuTestSample()
    [<Test>]
    member __.``D1 DAG Head Start`` () =
        for real in t.Reals do
            real.D1_DAGHeadStart() |> doChecks

    [<Test>]
    member __.``D2 DAG Tail Start`` () =
        for real in t.Reals do
            real.D2_DAGTailStart() |> doChecks

    [<Test>]
    member __.``D3 DAG Coin Relay`` () =
        for real in t.Reals do
            real.D3_DAGCoinRelay() |> doChecks

    [<Test>]
    member __.``D4 DAG Coin Reset`` () =
        for real in t.Reals do
            real.D4_DAGCoinReset() |> doChecks

