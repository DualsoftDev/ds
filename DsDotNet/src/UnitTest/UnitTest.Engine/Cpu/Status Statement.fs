namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Cpu


type StatusStatement() =
    do Fixtures.SetUpTest()

    [<Test>]
    member __.``S1 Ready`` () =
        Eq 1 1

    [<Test>]
    member __.``S2 Going`` () =
        Eq 1 1
          
    [<Test>]
    member __.``S3 Finish`` () =
        Eq 1 1
          
    [<Test>]
    member __.``S4 Homing`` () =
        Eq 1 1
          