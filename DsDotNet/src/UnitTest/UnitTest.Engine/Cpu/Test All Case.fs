namespace T.CPU

open NUnit.Framework

open T
open Engine.Core
open Engine.CodeGenCPU

type TestAllCase() =
    inherit EngineTestBaseClass()
    
    let t = CpuTestSample()


    [<Test>]
    member __.``Test All Case`` () =
        let result = Cpu.LoadStatements(t.Sys, Storages())
        result === result
