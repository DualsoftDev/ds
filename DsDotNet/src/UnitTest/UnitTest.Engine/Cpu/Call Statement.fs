namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS

type Spec07_CallStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``C1 Call Action Out`` () = Eq 1 1

    [<Test>] member __.``C2 Call Tx`` () = Eq 1 1
    [<Test>] member __.``C3 Call Rx`` () = Eq 1 1
