namespace UnitTest.Engine.Expression

open NUnit.Framework

open Engine.Parser.FS
open UnitTest.Engine
open System
open Engine.Core
open Engine.Cpu
open Engine.Obsolete.CpuUnit


[<AutoOpen>]
module TimerTestModule =

    type CpuTest() =
        do Fixtures.SetUpTest()

        [<Test>]
        member __.``TON creation test`` () =
            let t1 = PlcTag.Create("my_timer_control_tag", false)
            let condition = tag t1
            let statement, timer = CreateTON("myTon", condition, 100us)        // 20ms * 100 = 2sec
            timer.Struct.TT.Value === false
            timer.Struct.EN.Value === false
            timer.Struct.DN.Value === false
            timer.Struct.PRE.Value === 100us
            timer.Struct.ACC.Value === 0us

            // rung 입력 조건이 true
            t1.Value <- true
            statement.Do()

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
            statement.Do()
            timer.Struct.TT.Value === false
            timer.Struct.DN.Value === false
            timer.Struct.EN.Value === false
            timer.Struct.PRE.Value === 100us
            timer.Struct.ACC.Value === 0us

        [<Test>]
        member __.``TOF creation test`` () =
            let t1 = PlcTag.Create("my_timer_control_tag", false)
            let condition = tag t1
            let statement, timer = CreateTOF("myTof", condition, 100us)        // 20ms * 100 = 2sec
            timer.Struct.TT.Value === false
            timer.Struct.EN.Value === false
            timer.Struct.DN.Value === true
            timer.Struct.PRE.Value === 100us
            timer.Struct.ACC.Value === 100us

            // rung 입력 조건이 true : 무변화
            t1.Value <- true
            statement.Do()
            System.Threading.Thread.Sleep(100)

            timer.Struct.DN.Value === true
            timer.Struct.EN.Value === false
            timer.Struct.TT.Value === false

            // rung 입력 조건이 false
            t1.Value <- false
            statement.Do()

            System.Threading.Thread.Sleep(100)
            timer.Struct.DN.Value === true
            timer.Struct.EN.Value === false
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
            statement.Do()
            timer.Struct.TT.Value === false
            timer.Struct.DN.Value === false
            timer.Struct.EN.Value === false
            timer.Struct.PRE.Value === 100us
            timer.Struct.ACC.Value === 0us



