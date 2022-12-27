namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Cpu


type ButtonLampStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``B1 All Buttons`` () = Eq 1 1
    [<Test>] member __.``B2 All Lamps`` () = Eq 1 1
