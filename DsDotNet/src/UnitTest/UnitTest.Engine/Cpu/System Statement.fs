namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Cpu


type SystemStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``System Bit`` () = Eq 1 1
