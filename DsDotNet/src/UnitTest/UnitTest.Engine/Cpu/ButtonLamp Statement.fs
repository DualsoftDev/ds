namespace T.CPU

open NUnit.Framework

open Dual.Common.Core.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS

type Spec12_ButtonLampStatement() =
    inherit EngineTestBaseClass()

    let t = CpuTestSample()
    [<Test>]
    member __.``B1 HW Button Output`` () =
        t.Sys.B1_HWButtonOutput() |> doChecks
    [<Test>]
    member __.``B2 SW Button Output`` () =
        t.Sys.B2_SWButtonOutput() |> doChecks

    [<Test>]
    member __.``B3 HW Mode Lamp`` () =
        t.Sys.B3_HWModeLamp() |> doChecks   
    [<Test>]
    member __.``B4 SW Mode Lamp`` () =
        t.Sys.B4_SWModeLamp() |> doChecks
    [<Test>]
    member __.``B5 HWBtnConnetToSW`` () =
        t.Sys.B5_HWBtnConnetToSW() |> doChecks
        