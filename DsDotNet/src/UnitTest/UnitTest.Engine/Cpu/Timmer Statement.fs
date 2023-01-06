namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Cpu


type TimmerStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``T1 Delay Call`` () = Eq 1 1
