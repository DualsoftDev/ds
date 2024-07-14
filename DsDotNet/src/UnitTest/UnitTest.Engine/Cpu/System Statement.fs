namespace T.CPU

open NUnit.Framework

open Dual.Common.Core.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS


type Spec13_SystemStatement() =
    inherit EngineTestBaseClass()

    let t = CpuTestSample(WINDOWS)
    [<Test>]
    member __.``Y1 System Bit Set Flow`` () =
        t.Sys.Y1_SystemSimulationForFlow(t.Sys) |> doChecks

    //[<Test>]
    //member __.``Y2 System Condition Ready`` () =
    //    t.Sys.Y2_SystemConditionReady() |> doChecks

    //[<Test>]
    //member __.``Y3 System Condition Drive`` () =
    //    t.Sys.Y3_SystemConditionDrive() |> doChecks


