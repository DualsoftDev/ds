namespace UnitTest.Engine


open System.Linq
open Engine
open Engine.Core
open NUnit.Framework

[<AutoOpen>]
module ModelTests1 =
    type DemoTests1() = 
        do Fixtures.SetUpTest()

        [<Test>]
        member __.``Parse Cylinder`` () =
