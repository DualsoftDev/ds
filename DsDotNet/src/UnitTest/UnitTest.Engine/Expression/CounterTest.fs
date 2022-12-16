namespace UnitTest.Engine.Statement

open NUnit.Framework

open UnitTest.Engine
open Engine.Core
open UnitTest.Engine.Expression


[<AutoOpen>]
module CounterTestModule =

    type CounterTest() =
        do Fixtures.SetUpTest()

        let evaluateRungInputs (counter:Counter) =
            for s in counter.InputEvaluateStatements do
                s.Do()

        [<Test>]
        member __.``CTU creation test`` () =
            let t1 = PlcTag("my_counter_control_tag", false)
            let condition = tag2expr t1
            let ctu = CounterStatement.CreateCTU("myCTU", 100us, condition) |> counter
            ctu.OV.Value === false
            ctu.UN.Value === false
            ctu.DN.Value === false
            ctu.PRE.Value === 100us
            ctu.ACC.Value === 0us


            for i in [1..50] do
                t1.Value <- true
                evaluateRungInputs ctu
                ctu.ACC.Value === uint16 i
                t1.Value <- false
                evaluateRungInputs ctu
                ctu.DN.Value === false

            ctu.ACC.Value === 50us
            ctu.DN.Value === false
            for i in [51..100] do
                t1.Value <- true
                evaluateRungInputs ctu
                ctu.ACC.Value === uint16 i
                t1.Value <- false
                evaluateRungInputs ctu
            ctu.ACC.Value === 100us
            ctu.DN.Value === true

        [<Test>]
        member __.``CTU with reset creation test`` () =
            let t1 = PlcTag("my_counter_control_tag", false)
            let resetTag = PlcTag("my_counter_reset_tag", false)
            let condition = tag2expr t1
            let reset = tag2expr resetTag
            let ctu = CounterStatement.CreateCTU("myCTU", 100us, condition, reset) |> counter
            ctu.OV.Value === false
            ctu.UN.Value === false
            ctu.DN.Value === false
            ctu.RES.Value === false
            ctu.PRE.Value === 100us
            ctu.ACC.Value === 0us


            for i in [1..50] do
                t1.Value <- true
                evaluateRungInputs ctu
                ctu.ACC.Value === uint16 i
                t1.Value <- false
                evaluateRungInputs ctu
                ctu.DN.Value === false

            ctu.ACC.Value === 50us
            ctu.DN.Value === false

            // counter reset
            resetTag.Value <- true
            evaluateRungInputs ctu
            ctu.OV.Value === false
            ctu.UN.Value === false
            ctu.DN.Value === false
            ctu.RES.Value === true
            ctu.PRE.Value === 100us
            ctu.ACC.Value === 0us
