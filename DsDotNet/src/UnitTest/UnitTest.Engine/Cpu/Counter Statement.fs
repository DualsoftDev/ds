namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS


type Spec10_CounterStatement() =
    inherit EngineTestBaseClass()

    let t = CpuTestSample()
    [<Test>] 
    member __.``C1 Finish Ring Counter`` () = 
        t.Sys.C1_FinishRingCounter() |> doChecks
        
        
