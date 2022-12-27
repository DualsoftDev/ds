namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Cpu


type ExtensionStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``E1`` () = Eq 1 1
