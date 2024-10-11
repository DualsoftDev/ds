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

type Spec15_ErrorStatement() =
    inherit EngineTestBaseClass()
    let t = CpuTestSample(WINDOWS)

   
    [<Test>]
    member __.``E1 Call Error TX  Over Monitor`` () =
         for coin in t.Coins do
            coin.E1_CallErrTimeOver() |> doChecks

    [<Test>]
    member __.``E2 Call Error TX Shortage  Monitor`` () =
         for coin in t.Coins do
            coin.E2_CallErrTimeShortage() |> doChecks

    [<Test>]
    member __.``E3 Call Error RX Monitor`` () =
        for coin in t.InRealCalls do
            coin.V.E3_CallErrorRXMonitor() |> doChecks

    [<Test>]
    member __.``E4_Real Error Total Monitor`` () =
         for real in t.Reals do
            real.E4_RealErrorTotalMonitor() |> doCheck

    [<Test>]
    member __.``E5_Call Error Total Monitor`` () =
        for call in t.Coins do
            call.E5_CallErrorTotalMonitor() |> doCheck

