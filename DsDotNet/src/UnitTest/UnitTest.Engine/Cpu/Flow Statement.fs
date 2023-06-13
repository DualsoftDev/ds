namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open System.Linq


type Spec02_FlowStatement() =
    inherit EngineTestBaseClass()

    let t = CpuTestSample()

    [<Test>]
    member __.``F1 Root Start`` () =
        for real in t.Reals do
            real.F1_RootStart() |> doChecks

    [<Test>]
    member __.``F2 Root Reset`` () =
        for real in t.Reals do
            real.F2_RootReset() |> doChecks

    [<Test>]
    member __.``F3 Root Going Relay`` () =
        for real in t.Reals do
            real.F3_RootGoingRelay() |> doChecks

    [<Test>]
    member __.``F4 Root Reset Coin`` () =
        for coin in t.Coins do
            coin.F4_RootCoinRelay()   |> doCheck
