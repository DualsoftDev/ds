namespace T.CPU

open NUnit.Framework

open Engine.Parser.FS
open T
open System
open Engine.Core
open Engine.Cpu


type StatusTest() =
    do Fixtures.SetUpTest()

    [<Test>]
    member __.``1 Ready`` () =
        Eq 1 1

    [<Test>]
    member __.``2 Going`` () =
        Eq 1 1
          
    [<Test>]
    member __.``3 Finish`` () =
        Eq 1 1
          
    [<Test>]
    member __.``4 Homing`` () =
        Eq 1 1
          