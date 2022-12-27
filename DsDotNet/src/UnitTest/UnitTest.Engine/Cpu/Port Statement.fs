namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Cpu


type PortStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``P1 Real Start Port`` () = Eq 1 1
    [<Test>] member __.``P2 Real Reset Port`` () = Eq 1 1
    [<Test>] member __.``P3 Real End Port`` () = Eq 1 1
    [<Test>] member __.``P4 Call Start Port`` () = Eq 1 1
    [<Test>] member __.``P5 Call Reset Port`` () = Eq 1 1
    [<Test>] member __.``P6 Call End Port`` () = Eq 1 1
