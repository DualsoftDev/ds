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
    member __.``M5_Real Error Total Monitor`` () =
         for real in t.Reals do
            real.M5_RealErrorTotalMonitor() |> doChecks

    [<Test>]
    member __.``M6_Call Error Total Monitor`` () =
        for call in t.Coins do
            call.M6_CallErrorTotalMonitor() |> doCheck

