namespace UnitTest.Engine.Statement

open NUnit.Framework

open UnitTest.Engine
open Engine.Core
open Engine.Parser.FS.ExpressionParser


[<AutoOpen>]
module StatementTestModule =

    type StatementTest() =
        do Fixtures.SetUpTest()

        [<Test>]
        member __.``X CTU creation test`` () =
            let t1 = PlcTag("my_counter_control_tag", false)
            let xxx = "ctu myCounter = createCTU(false, 100us)" |> parseStatement
            ()




