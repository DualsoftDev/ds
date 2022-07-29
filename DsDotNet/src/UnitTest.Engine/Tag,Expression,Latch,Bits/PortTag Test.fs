namespace UnitTest.Engine


open Xunit
open Engine.Core
open Dual.Common
open Xunit.Abstractions
open System

[<AutoOpen>]
module PortExpressionTest =
    type PortExpressionTests1(output1:ITestOutputHelper) =

        let init() =
            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit
                    logDebug $"Bit changed: [{bit}] = {bc.NewValue}"
                )
            |> ignore

        interface IClassFixture<Fixtures.DemoFixture>

        [<Fact>]
        member __.``PortExpression test`` () =
            logInfo "============== PortExpression test"
            init()


            let cpu = new Cpu("dummy", new Model())


            let ``_PortExpressionStart 테스트`` =
                let plan = new Tag(cpu, null, "T1_test1")
                let actual = new Tag(cpu, null, "T2_test1")
                let pts = new PortExpressionStart(cpu, "PortExpressionStart", plan, actual)
                pts.Value === false
                pts.Plan.Value === false
                pts.Actual.Value === false

                // Expression 으로, 값을 설정할 수 없어야 한다.
                (fun () -> pts.Value <- true)
                |> ShouldFail

                plan.Value <- true
                pts.Plan.Value === true
                pts.Value === true
                pts.Actual.Value === true

                // plan OFF 시, actual tag 도 OFF 되어야 한다.
                plan.Value <- false
                pts.Plan.Value === false
                pts.Value === false
                pts.Actual.Value === false


                // pts plan tag ON 시, pts 도 ON 되어야 한다.
                plan.Value <- true
                pts.Plan.Value === true
                pts.Value === true
                pts.Actual.Value === true

            let ``_PortExpressionEnd Normal 테스트`` =
                let actual = new Tag(cpu, null, "T2_test2")

                let pte = PortExpressionEnd.Create(cpu, "_PortExpressionEnd", actual)
                let plan = pte.Plan
                pte.Value === false
                pte.Plan.Value === false
                pte.Actual.Value === false

                // pte 전체 ON 하더라도, actual tag 는 ON 되지 않는다.
                plan.Value <- true
                pte.Plan.Value === true
                pte.Value === false
                pte.Actual.Value === false

                // actual tag ON 시, pte 전체 ON
                actual.Value <- true
                pte.Value === true

                // actual tag 흔들림시, pte 전체도 연동
                (fun () -> actual.Value <- false)
                |> ShouldFailWithSubstringT<DsException> "Spatial Error:"

                actual.Value === false
                pte.Value === false


            let ``_PortExpressionEnd 특이 case 테스트`` =
                let actual = new Tag(cpu, null, "T2_test3")
                actual.Value <- true

                // Actual 이 ON 인 상태에서의 creation
                let pte = PortExpressionEnd.Create(cpu, "_PortExpressionEnd", actual)
                let plan = pte.Plan
                pte.Value === false
                pte.Plan.Value === false
                pte.Actual.Value === true

                // actual tag ON 상태에서 plan 만 ON 시킬 수 없다.
                (fun () -> pte.Value <- true)
                |> ShouldFailWithSubstringT<DsException> "Spatial Error:"

                pte.Value === false
                plan.Value === false


                // actual tag OFF 상태에서는 plan OFF 가능
                actual.Value <- false
                plan.Value <- true
                pte.Plan.Value === true
                pte.Actual.Value === false
                pte.Value === false

                pte.Actual.Value <- true
                pte.Value === true

                ()
            ()

