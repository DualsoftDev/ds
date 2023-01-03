namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Cpu


type CallStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``C1 Call Action Out`` () = Eq 1 1
    [<Test>] member __.``C2 Call Head Complete`` () = Eq 1 1
    [<Test>] member __.``C3 Call Tail Complete`` () = Eq 1 1
    [<Test>] member __.``C4 Call Tx`` () = Eq 1 1
    [<Test>] member __.``C5 Call Rx`` () = Eq 1 1
