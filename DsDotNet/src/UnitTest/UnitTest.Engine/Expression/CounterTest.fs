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
            let storages = Storages()
            let t1 = PlcTag("my_counter_control_tag", "%M1.1", false)
            let condition = tag2expr t1
            let ctu = CounterStatement.CreateCTU(storages, "myCTU", 100us, condition) |> toCounter
            ctu.OV.Value === false
            ctu.UN.Value === false
            ctu.DN.Value === false
            ctu.PRE.Value === 100us
            ctu.ACC.Value === 0us


            (* Counter struct 의 내부 tag 들이 생성되고, 등록되었는지 확인 *)
            let internalTags =
                [
                    // CTU 및 CTD 에서는 .CU 와 .CD tag 는 internal 로 숨겨져 있다.
                    ctu.OV :> IStorage
                    ctu.UN
                    ctu.DN
                    ctu.PRE
                    ctu.ACC
                    ctu.RES
                ]

            storages.ContainsKey("myCTU") === true
            for t in internalTags do
                storages.ContainsKey(t.Name) === true


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
        member __.``CTUD creation test`` () =
            let storages = Storages()
            let t1 = PlcTag("my_counter_up_tag", "%M1.1", false)
            let t2 = PlcTag("my_counter_down_tag", "%M1.1", false)
            let t3 = PlcTag("my_counter_reset_tag", "%M1.1", false)
            let upCondition = tag2expr t1
            let downCondition = tag2expr t2
            let resetCondition = tag2expr t3
            let accum = 50us

            let ctu = CounterStatement.CreateCTUD(storages, "myCTU", 100us, upCondition, downCondition, resetCondition, accum) |> toCounter
            ctu.OV.Value === false
            ctu.UN.Value === false
            ctu.DN.Value === false
            ctu.PRE.Value === 100us
            ctu.ACC.Value === 0us


            (* Counter struct 의 내부 tag 들이 생성되고, 등록되었는지 확인 *)
            let internalTags =
                [
                    ctu.CU :> IStorage
                    ctu.CD
                    ctu.OV
                    ctu.UN
                    ctu.DN
                    ctu.PRE
                    ctu.ACC
                    ctu.RES
                ]

            storages.ContainsKey("myCTU") === true
            for t in internalTags do
                storages.ContainsKey(t.Name) === true

        [<Test>]
        member __.``CTU with reset creation test`` () =
            let t1 = PlcTag("my_counter_control_tag", "%M1.1", false)
            let resetTag = PlcTag("my_counter_reset_tag", "%M1.1", false)
            let condition = tag2expr t1
            let reset = tag2expr resetTag
            let ctu = CounterStatement.CreateCTU(emptyStorages, "myCTU", 100us, condition, reset) |> toCounter
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
