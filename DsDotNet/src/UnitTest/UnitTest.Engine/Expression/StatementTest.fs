namespace UnitTest.Engine.Statement

open NUnit.Framework

open UnitTest.Engine
open Engine.Core
open Engine.Parser.FS.ExpressionParser
open UnitTest.Engine.Expression


[<AutoOpen>]
module StatementTestModule =

    type StatementTest() =
        do Fixtures.SetUpTest()

        let toTimer = timer
        [<Test>]
        member __.``CTU creation test`` () =
            let rungConditionInTag = PlcTag("my_timer_control_tag", true)
            let resetTag = PlcTag("my_timer_reset_tag", false)
            let condition = tag rungConditionInTag
            let reset = tag resetTag
            let timerStatement = TimerStatement.CreateRTO("myRto", 2000us, condition, reset)
            let timer = toTimer timerStatement
            ()

        [<Test>]
        member __.``CTU/TON creation test`` () =
            let t1 = PlcTag("my_counter_control_tag", false)
            let coutnerStatement:Statement = "ctu myCounter = createCTU(100us, false)" |> parseStatement
            let counter = counter coutnerStatement
            let timerStatement2:Statement = "ton myTimer = createTON(100us, false)" |> parseStatement
            ()




