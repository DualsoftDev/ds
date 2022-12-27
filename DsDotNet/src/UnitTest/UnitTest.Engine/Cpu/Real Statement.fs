namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Cpu


type RealStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``R1 Real Initial Start`` () = Eq 1 1
    [<Test>] member __.``R2 Real Job Complete`` () = Eq 1 1
