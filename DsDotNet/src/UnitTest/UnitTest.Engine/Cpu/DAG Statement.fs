namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Cpu


type DAGStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``D1 DAG Initial Start`` () = Eq 1 1
    [<Test>] member __.``D2 DAG Tail Start`` () = Eq 1 1
