namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Cpu


type OperationModeStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``O1 Emergency Operation Mode`` () = Eq 1 1
    [<Test>] member __.``O2 Stop Operation Mode`` () = Eq 1 1
    [<Test>] member __.``O3 Manual Operation Mode `` () = Eq 1 1
    [<Test>] member __.``O4 Run Operation Mode `` () = Eq 1 1
    [<Test>] member __.``O5 Dry Run Operation Mode `` () = Eq 1 1
