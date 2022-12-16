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
            let rungConditionInTag = PlcTag("my_timer_control_tag", true)
            let resetTag = PlcTag("my_timer_reset_tag", false)
            let condition = tag rungConditionInTag
            let reset = tag resetTag
            let timerStatement = TimerStatement.CreateRTO("myRto", 2000us, condition, reset)
            let timer = timerStatement.Timer

            let t1 = PlcTag("my_counter_control_tag", false)
            let xxx = "ctu myCounter = createCTU(false, 100us)" |> parseStatement
            ()




