namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Cpu


type CounterStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``C1 Finish Ring Counter`` () = Eq 1 1
