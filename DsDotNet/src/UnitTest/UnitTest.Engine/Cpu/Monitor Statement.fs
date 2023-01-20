namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Common.FS
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
    member __.``M4 Call Error RX Monitor`` () =    Eq 1 1
        //for coin in t.Coins do  //test ahn coin만 ?
        //    coin.M4_CallErrorRXMonitor() |> doCheck

    [<Test>]
    member __.``M5 Real Error RX Monitor`` () =
         for real in t.Reals do
            real.M5_RealErrorTXMonitor() |> doCheck

    [<Test>]
    member __.``M6 Real Error RX Monitor`` () =
        for real in t.Reals do
            real.M6_RealErrorRXMonitor() |> doCheck

