namespace UnitTest.Engine.Statement

open NUnit.Framework

open UnitTest.Engine
open Engine.Core
open Engine.Obsolete.CpuUnit


[<AutoOpen>]
module CounterTestModule =

    type CounterTest() =
        do Fixtures.SetUpTest()

        let evaluateRungInputs (counter:Counter) =
            for s in counter.InputEvaluateStatements do
                s.Do()
        [<Test>]
        member __.``CTU creation test`` () =
            let t1 = PlcTag.Create("my_counter_control_tag", false)
            let condition = tag t1
            let ctu = CreateCTU("myCTU", condition, 100us)
            ctu.OV.Value === false
            ctu.UN.Value === false
            ctu.DN.Value === false
            ctu.PRE.Value === 100us
            ctu.ACC.Value === 0us


