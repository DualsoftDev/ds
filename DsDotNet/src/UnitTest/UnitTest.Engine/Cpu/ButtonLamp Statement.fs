namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS

type Spec12_ButtonLampStatement() =
    inherit EngineTestBaseClass()

    let t = CpuTestSample()
    do
            t.GenerationIO()
    [<Test>]
    member __.``B1 Button Output`` () =
        t.Sys.B1_ButtonOutput() |> doChecks

    [<Test>]
    member __.``B2 Mode Lamp`` () =
        t.Sys.B2_ModeLamp() |> doChecks
