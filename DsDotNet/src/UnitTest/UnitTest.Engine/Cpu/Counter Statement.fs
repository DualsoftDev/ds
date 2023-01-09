namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS


type Spec10_CounterStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``C1 Finish Ring Counter`` () = Eq 1 1
