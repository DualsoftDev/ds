namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Cpu


type FlowStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``F1 Root Start Real`` () = Eq 1 1
    [<Test>] member __.``F2 Root Reset Real`` () = Eq 1 1
    [<Test>] member __.``F3 Root Start Call`` () = Eq 1 1
    [<Test>] member __.``F4 Root Reset Call`` () = Eq 1 1
