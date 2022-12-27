namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Cpu


type TimmerStatement() =
    do Fixtures.SetUpTest()

    [<Test>] member __.``T1 Delay Input`` () = Eq 1 1
    [<Test>] member __.``T2 Sustain Output`` () = Eq 1 1
