namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS

type Spec14_ExtensionStatement() =
    inherit EngineTestBaseClass()

    let t = CpuTestSample()
    do
            t.GenerationIO()
    [<Test>] member __.``E1`` () = Eq 1 1
