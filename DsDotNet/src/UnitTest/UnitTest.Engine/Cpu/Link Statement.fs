namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Cpu


type LinkStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``L1 Link Start`` () = Eq 1 1
    [<Test>] member __.``L2 Link Reset`` () = Eq 1 1
    [<Test>] member __.``L2 Link StartReset`` () = Eq 1 1
