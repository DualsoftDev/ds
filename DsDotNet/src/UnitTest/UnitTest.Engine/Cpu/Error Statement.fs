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
    member __.``E2_CallErrRXMonitor`` () =
         for coin in t.Coins do
            coin.E2_CallErrRXMonitor() |> doChecks

    [<Test>]
    member __.``E3_CallErrTotalMonitor`` () =
        for coin in t.InRealCalls do
            coin.V.E3_CallErrTotalMonitor() |> doCheck

    [<Test>]
    member __.``E4_RealErrTotalMonitor`` () =
         for real in t.Reals do
            real.E4_RealErrTotalMonitor() |> doCheck

    