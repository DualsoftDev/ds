namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Cpu


type FlowStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``F1 Root Start`` () = Eq 1 1
    [<Test>] member __.``F2 Root Reset`` () = Eq 1 1
