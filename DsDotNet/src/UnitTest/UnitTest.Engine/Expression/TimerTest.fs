namespace UnitTest.Engine.Expression

open NUnit.Framework

open UnitTest.Engine
open Engine.Core
open Engine.Obsolete.CpuUnit


[<AutoOpen>]
module TimerTestModule =

    type CpuTest() =
        do Fixtures.SetUpTest()

        let applyRungConditinInChange (timer:Timer) = timer.ConditionCheckStatement.Value.Do()
        [<Test>]
        member __.``TON creation test`` () =
            let t1 = PlcTag.Create("my_timer_control_tag", false)
            let condition = tag t1
            let timer = CreateTON("myTon", condition, 100us)        // 20ms * 100 = 2sec
            timer.Struct.TT.Value === false
            timer.Struct.EN.Value === false
            timer.Struct.DN.Value === false
            timer.Struct.PRE.Value === 100us
            timer.Struct.ACC.Value === 0us

            // rung 입력 조건이 true
            t1.Value <- true
            applyRungConditinInChange timer

            timer.Struct.DN.Value === false
            timer.Struct.EN.Value === true
            timer.Struct.TT.Value === true

            // 설정된 timer 시간 경과를 기다림
            System.Threading.Thread.Sleep(2100)
            timer.Struct.TT.Value === false
            timer.Struct.DN.Value === true
            timer.Struct.EN.Value === true
            timer.Struct.PRE.Value === 100us
            timer.Struct.ACC.Value === 100us


            // rung 입력 조건이 false
            t1.Value <- false
            applyRungConditinInChange timer
            timer.Struct.TT.Value === false
            timer.Struct.DN.Value === false
            timer.Struct.EN.Value === false
            timer.Struct.PRE.Value === 100us
            timer.Struct.ACC.Value === 0us

        [<Test>]
        member __.``TOF creation with initial TRUE test`` () =
            let t1 = PlcTag.Create("my_timer_control_tag", true)
            let condition = tag t1
            let timer = CreateTOF("myTof", condition, 100us)        // 20ms * 100 = 2sec
            timer.Struct.EN.Value === true
            timer.Struct.TT.Value === false
            timer.Struct.DN.Value === true
            timer.Struct.PRE.Value === 100us
            timer.Struct.ACC.Value === 0us

        [<Test>]
        member __.``TOF creation with initial FALSE test`` () =
            let t1 = PlcTag.Create("my_timer_control_tag", false)
            let condition = tag t1
            let timer = CreateTOF("myTof", condition, 100us)        // 20ms * 100 = 2sec
            timer.Struct.TT.Value === false
            timer.Struct.EN.Value === false
            timer.Struct.DN.Value === false
            timer.Struct.PRE.Value === 100us
            timer.Struct.ACC.Value === 0us

        [<Test>]
        member __.``TOF creation with t -> f -> t -> F -> t test`` () =
            let t1 = PlcTag.Create("my_timer_control_tag", true)
            let condition = tag t1
            let timer = CreateTOF("myTof", condition, 100us)        // 20ms * 100 = 2sec
            // rung 입력 조건이 false
            t1.Value <- false
            applyRungConditinInChange timer

            timer.Struct.EN.Value === false
            timer.Struct.TT.Value === true
            timer.Struct.DN.Value === true
            timer.Struct.PRE.Value === 100us
            (0us <= timer.Struct.ACC.Value && timer.Struct.ACC.Value <= 50us) === true

            t1.Value <- true
            applyRungConditinInChange timer
            System.Threading.Thread.Sleep(500)
            timer.Struct.EN.Value === true
            timer.Struct.TT.Value === false
            timer.Struct.DN.Value === true

            t1.Value <- false
            applyRungConditinInChange timer
            System.Threading.Thread.Sleep(500)
            timer.Struct.EN.Value === false
            timer.Struct.TT.Value === true
            timer.Struct.DN.Value === true

            System.Threading.Thread.Sleep(2100)
            timer.Struct.EN.Value === false
            timer.Struct.TT.Value === false
            timer.Struct.DN.Value === false
            timer.Struct.ACC.Value === 100us

            t1.Value <- true
            applyRungConditinInChange timer
            System.Threading.Thread.Sleep(100)
            timer.Struct.EN.Value === true
            timer.Struct.TT.Value === false
            timer.Struct.DN.Value === true
            timer.Struct.ACC.Value <= 50us === true

