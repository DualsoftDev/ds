namespace UnitTest.Engine.Statement

open NUnit.Framework

open UnitTest.Engine
open Engine.Core
open UnitTest.Engine.Expression

[<AutoOpen>]
module TimerTestModule =

    type TimerTest() =
        do Fixtures.SetUpTest()

        let evaluateRungInputs (timer:Timer) =
            for s in timer.InputEvaluateStatements do
                s.Do()
        [<Test>]
        member __.``TON creation test`` () =
            let t1 = PlcTag("my_timer_control_tag", false)
            let condition = tag t1
            let timer = TimerStatement.CreateTON("myTon", 2000us, condition) |> timer       // 2000ms = 2sec
            timer.TT.Value === false
            timer.EN.Value === false
            timer.DN.Value === false
            timer.PRE.Value === 2000us
            timer.ACC.Value === 0us

            // rung 입력 조건이 true
            t1.Value <- true
            evaluateRungInputs timer

            timer.DN.Value === false
            timer.EN.Value === true
            timer.TT.Value === true

            // 설정된 timer 시간 경과를 기다림
            System.Threading.Thread.Sleep(2100)
            timer.TT.Value === false
            timer.DN.Value === true
            timer.EN.Value === true
            timer.PRE.Value === 2000us
            timer.ACC.Value === 2000us


            // rung 입력 조건이 false
            t1.Value <- false
            evaluateRungInputs timer
            timer.TT.Value === false
            timer.DN.Value === false
            timer.EN.Value === false
            timer.PRE.Value === 2000us
            timer.ACC.Value === 0us

        [<Test>]
        member __.``TOF creation with initial TRUE test`` () =
            let t1 = PlcTag("my_timer_control_tag", true)
            let condition = tag t1
            let timer = TimerStatement.CreateTOF("myTof", 2000us, condition) |> timer        // 2000ms = 2sec
            timer.EN.Value === true
            timer.TT.Value === false
            timer.DN.Value === true
            timer.PRE.Value === 2000us
            timer.ACC.Value === 0us

        [<Test>]
        member __.``TOF creation with initial FALSE test`` () =
            let t1 = PlcTag("my_timer_control_tag", false)
            let condition = tag t1
            let timer = TimerStatement.CreateTOF("myTof", 2000us, condition) |> timer        // 2000ms = 2sec
            timer.TT.Value === false
            timer.EN.Value === false
            timer.DN.Value === false
            timer.PRE.Value === 2000us
            timer.ACC.Value === 0us

        [<Test>]
        member __.``TOF creation with t -> f -> t -> F -> t test`` () =
            let t1 = PlcTag("my_timer_control_tag", true)
            let condition = tag t1
            let timer = TimerStatement.CreateTOF("myTof", 2000us, condition) |> timer        // 2000ms = 2sec
            // rung 입력 조건이 false
            t1.Value <- false
            evaluateRungInputs timer

            timer.EN.Value === false
            timer.TT.Value === true
            timer.DN.Value === true
            timer.PRE.Value === 2000us
            (0us <= timer.ACC.Value && timer.ACC.Value <= 1000us) === true

            t1.Value <- true
            evaluateRungInputs timer
            System.Threading.Thread.Sleep(500)
            timer.EN.Value === true
            timer.TT.Value === false
            timer.DN.Value === true

            t1.Value <- false
            evaluateRungInputs timer
            System.Threading.Thread.Sleep(500)
            timer.EN.Value === false
            timer.TT.Value === true
            timer.DN.Value === true

            System.Threading.Thread.Sleep(2100)
            timer.EN.Value === false
            timer.TT.Value === false
            timer.DN.Value === false
            timer.ACC.Value === 2000us

            t1.Value <- true
            evaluateRungInputs timer
            System.Threading.Thread.Sleep(100)
            timer.EN.Value === true
            timer.TT.Value === false
            timer.DN.Value === true
            timer.ACC.Value <= 1000us === true


        [<Test>]
        member __.``RTO creation test`` () =
            let rungConditionInTag = PlcTag("my_timer_control_tag", true)
            let resetTag = PlcTag("my_timer_reset_tag", false)
            let condition = tag rungConditionInTag
            let reset = tag resetTag
            let timer = TimerStatement.CreateRTO("myRto", 2000us, condition, reset) |> timer        // 2000ms = 2sec

            timer.EN.Value === true
            timer.TT.Value === true
            timer.DN.Value === false
            timer.PRE.Value === 2000us
            timer.ACC.Value <= 1000us === true
            timer.RES.Value === false

            // rung 입력 조건이 false : Pause
            rungConditionInTag.Value <- false
            evaluateRungInputs timer
            timer.EN.Value === false
            timer.TT.Value === false
            System.Threading.Thread.Sleep(2100)
            timer.ACC.Value < 2000us === true
            timer.DN.Value === false

            // rung 입력 조건이 false
            rungConditionInTag.Value <- true
            evaluateRungInputs timer
            timer.EN.Value === true
            timer.TT.Value === true
            timer.DN.Value === false
            System.Threading.Thread.Sleep(2100)
            timer.DN.Value === true
            timer.ACC.Value === 2000us


            // reset 입력 조건이 true
            resetTag.Value <- true
            evaluateRungInputs timer
            timer.EN.Value === true
            timer.ACC.Value === 0us
