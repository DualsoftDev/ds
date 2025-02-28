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
    member __.``Y1 System ActiveBtn For PassiveFlow`` () =
        t.Sys.Y1_SystemActiveBtnForPassiveFlow(t.Sys) |> doChecks

    [<Test>]
    member __.``Y2 SystemPause`` () =
        t.Sys.Y2_SystemPause() |> doCheck

    [<Test>]
    member __.``Y3 SystemState`` () =
        t.Sys.Y3_SystemState() |> doChecks

    [<Test>]
    member __.``Y4 SystemConditionError`` () =
        t.Sys.Y4_SystemConditionError() |> doChecks

    [<Test>]
    member __.``Y5 SystemEmgAlramError`` () =
        t.Sys.GenerationButtonEmergencyMemory()
        t.Sys.Y5_SystemEmgAlramError() |> doChecks

    [<Test>]
    member __.``Y6 SystemClearBtnForFlow`` () =
        t.Sys.Y6_SystemClearBtnForFlow() |> doChecks
