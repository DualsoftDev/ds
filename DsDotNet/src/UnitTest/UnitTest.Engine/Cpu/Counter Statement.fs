namespace T.CPU

open NUnit.Framework

open Dual.Common.Core.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS


type Spec10_CounterStatement() =
    inherit EngineTestBaseClass()

    let t = CpuTestSample()
    [<Test>] //test ahn real 반복으로 수정 필요
    member __.``C1 Finish Ring Counter`` () = true
        //t.Sys.C1_FinishRingCounter() |> doChecks


