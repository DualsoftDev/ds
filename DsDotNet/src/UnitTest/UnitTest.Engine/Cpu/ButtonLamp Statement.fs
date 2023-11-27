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
    member __.``B1 Button Output`` () =
        t.Sys.B1_HWButtonOutput() |> doChecks

    [<Test>]
    member __.``B2 Mode Lamp`` () =
        t.Sys.B2_HWLamp() |> doChecks
    [<Test>]
    member __.``B3 HWBtnConnetToSW`` () =
        t.Sys.B3_HWBtnConnetToSW() |> doChecks
        