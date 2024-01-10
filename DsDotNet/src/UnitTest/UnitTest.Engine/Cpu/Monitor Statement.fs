namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Dual.Common.Core.FS
open Engine.Cpu
open Engine.CodeGenCPU
open System.Linq

type Spec05_MonitorStatement() =
    inherit EngineTestBaseClass()
    let t = CpuTestSample()

    [<Test>]
    member __.``M1 Origin Monitor`` () =
        for real in t.Reals do
            real.M1_OriginMonitor() |> doCheck

    [<Test>]
    member __.``M2 Pause Monitor`` () =
        for v in t.ALL do
            v.M2_PauseMonitor() |> doCheck

    [<Test>]
    member __.``M3 Call Error TX Monitor`` () =
         for coin in t.Coins do
            coin.M3_CallErrorTXMonitor() |> doChecks

    [<Test>]
    member __.``M4 Call Error RX Monitor`` () =
        for coin in t.InRealCalls do
            coin.V.M4_CallErrorRXMonitor() |> doChecks

    [<Test>]
    member __.``M5 Real Error TX Monitor`` () =
         for real in t.Reals do
            real.M5_RealErrorTXMonitor() |> doCheck

    [<Test>]
    member __.``M6 Real Error RX Monitor`` () =
        for real in t.Reals do
            real.M6_RealErrorRXMonitor() |> doCheck

