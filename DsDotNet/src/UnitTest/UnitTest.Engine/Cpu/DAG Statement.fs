namespace T.CPU

open NUnit.Framework

open Engine.Common.FS
open T
open Engine.Core
open Engine.Cpu
open Engine.CodeGenCPU
open Engine.Parser.FS


type Spec08_DAGStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``D1 DAG Head Start`` () = Eq 1 1
    [<Test>] member __.``D2 DAG Tail Start`` () = Eq 1 1
    [<Test>] member __.``D3 DAG Complete`` () = Eq 1 1
