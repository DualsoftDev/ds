namespace UnitTest.Engine.Expression

open NUnit.Framework

open UnitTest.Engine
open Engine.Core
open Engine.Obsolete.CpuUnit


[<AutoOpen>]
module TimerTestModule =

    type TimerTest() =
        do Fixtures.SetUpTest()

        let evaluateRungInputs (timer:Timer) =
            for s in timer.InputEvaluateStatements do
                s.Do()
        [<Test>]
        member __.``TON creation test`` () =
            let t1 = PlcTag.Create("my_timer_control_tag", false)
            let condition = tag t1
            let timer = CreateTON("myTon", condition, 100us)        // 20ms * 100 = 2sec
            timer.TT.Value === false
            timer.EN.Value === false
            timer.DN.Value === false
            timer.PRE.Value === 100us
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
            timer.PRE.Value === 100us
            timer.ACC.Value === 100us


            // rung 입력 조건이 false
            t1.Value <- false
            evaluateRungInputs timer
            timer.TT.Value === false
            timer.DN.Value === false
            timer.EN.Value === false
            timer.PRE.Value === 100us
            timer.ACC.Value === 0us

        [<Test>]
        member __.``TOF creation with initial TRUE test`` () =
            let t1 = PlcTag.Create("my_timer_control_tag", true)
            let condition = tag t1
            let timer = CreateTOF("myTof", condition, 100us)        // 20ms * 100 = 2sec
            timer.EN.Value === true
            timer.TT.Value === false
            timer.DN.Value === true
            timer.PRE.Value === 100us
            timer.ACC.Value === 0us

        [<Test>]
        member __.``TOF creation with initial FALSE test`` () =
            let t1 = PlcTag.Create("my_timer_control_tag", false)
            let condition = tag t1
            let timer = CreateTOF("myTof", condition, 100us)        // 20ms * 100 = 2sec
            timer.TT.Value === false
            timer.EN.Value === false
            timer.DN.Value === false
            timer.PRE.Value === 100us
            timer.ACC.Value === 0us

        [<Test>]
        member __.``TOF creation with t -> f -> t -> F -> t test`` () =
            let t1 = PlcTag.Create("my_timer_control_tag", true)
            let condition = tag t1
            let timer = CreateTOF("myTof", condition, 100us)        // 20ms * 100 = 2sec
            // rung 입력 조건이 false
            t1.Value <- false
            evaluateRungInputs timer

            timer.EN.Value === false
            timer.TT.Value === true
            timer.DN.Value === true
            timer.PRE.Value === 100us
            (0us <= timer.ACC.Value && timer.ACC.Value <= 50us) === true

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
            timer.ACC.Value === 100us

            t1.Value <- true
            evaluateRungInputs timer
            System.Threading.Thread.Sleep(100)
            timer.EN.Value === true
            timer.TT.Value === false
            timer.DN.Value === true
            timer.ACC.Value <= 50us === true


        [<Test>]
        member __.``RTO creation test`` () =
            let rungConditionInTag = PlcTag.Create("my_timer_control_tag", true)
            let resetTag = PlcTag.Create("my_timer_reset_tag", false)
            let condition = tag rungConditionInTag
            let reset = tag resetTag
            let timer = CreateRTO("myRto", condition, reset, 100us)        // 20ms * 100 = 2sec

            timer.EN.Value === true
            timer.TT.Value === true
            timer.DN.Value === false
            timer.PRE.Value === 100us
            timer.ACC.Value <= 50us === true
            timer.RES.Value === false

            // rung 입력 조건이 false : Pause
            rungConditionInTag.Value <- false
            evaluateRungInputs timer
            timer.EN.Value === false
            timer.TT.Value === false
            System.Threading.Thread.Sleep(2100)
            timer.ACC.Value < 100us === true
            timer.DN.Value === false

            // rung 입력 조건이 false
            rungConditionInTag.Value <- true
            evaluateRungInputs timer
            timer.EN.Value === true
            timer.TT.Value === true
            timer.DN.Value === false
            System.Threading.Thread.Sleep(2100)
            timer.DN.Value === true
            timer.ACC.Value === 100us


            // reset 입력 조건이 true
            resetTag.Value <- true
            evaluateRungInputs timer
            timer.EN.Value === true
            timer.ACC.Value === 0us
