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
    member __.``E3_CallErrRXInterlockMonitor`` () =
        for coin in t.Coins do
            coin.E3_CallErrRXInterlockMonitor() |> doChecks

    [<Test>]
    member __.``E4_CallErrTotalMonitor`` () =
         for c in t.InRealCalls do
            c.VC.E4_CallErrTotalMonitor() |> doCheck
    
    [<Test>]
    member __.``E5_RealErrTotalMonitor`` () =
         for real in t.Reals do
            real.E5_RealErrTotalMonitor() |> doCheck

    