namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Cpu


type DAGStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``D1 DAG Head Start`` () = Eq 1 1
    [<Test>] member __.``D2 DAG Head Complete`` () = Eq 1 1
    [<Test>] member __.``D3 DAG Tail Start`` () = Eq 1 1
    [<Test>] member __.``D4 DAG Tail Complete`` () = Eq 1 1
