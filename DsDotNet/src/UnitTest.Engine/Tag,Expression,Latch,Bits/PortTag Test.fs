namespace UnitTest.Engine


open Xunit
open Engine.Core
open Dual.Common
open Xunit.Abstractions
open System
open System.Threading

[<AutoOpen>]
module PortInfoTest =
    type PortInfoTests1(output1:ITestOutputHelper) =

        let init() =
            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit
                    logDebug $"Bit changed: [{bit}] = {bc.NewValue}"
                )
            |> ignore

        interface IClassFixture<Fixtures.DemoFixture>

        [<Fact>]
        member __.``PortInfo test`` () =
            logInfo "============== PortInfo test"
            init()


            let ``_PortInfoStart 테스트`` =
                let cpu = new Cpu("dummy", new Model())
                let plan = new Tag(cpu, null, "T1_test1")
                let actual = new Tag(cpu, null, "T2_test1")
                let pts = new PortInfoStart(cpu, null, "PortInfoStart", plan, actual)


                let wait() = wait(cpu)
                let enqueue(bit, value) =
                    cpu.Enqueue(bit, value)
                    wait()

                cpu.BuildBitDependencies()
                let runSubscription = cpu.Run()

                pts.Value === false
                pts.Plan.Value === false
                pts.Actual.Value === false

                //// Expression 으로, 값을 설정할 수 없어야 한다.
                //(fun () -> enqueue(pts, true))
                //|> ShouldFail

                enqueue(pts, true)
                pts.Plan.Value === true
                pts.Value === true
                pts.Actual.Value === true

                // plan OFF 시, actual tag 도 OFF 되어야 한다.
                enqueue(plan, false)
                pts.Plan.Value === false
                pts.Value === false
                pts.Actual.Value === false


                // pts plan tag ON 시, pts 도 ON 되어야 한다.
                enqueue(plan, true)
                pts.Plan.Value === true
                pts.Value === true
                pts.Actual.Value === true

            let ``_PortInfoEnd Normal 테스트`` =
                let cpu = new Cpu("dummy", new Model())
                let actual = new Tag(cpu, null, "T2_test2")

                let pte = PortInfoEnd.Create(cpu, null, "_PortInfoEnd_test2", actual)

                let wait() = wait(cpu)
                let enqueue(bit, value) =
                    cpu.Enqueue(bit, value)
                    wait()

                cpu.BuildBitDependencies()
                let runSubscription = cpu.Run()

                let plan = pte.Plan
                pte.Value === false
                pte.Plan.Value === false
                pte.Actual.Value === false

                // pte 전체 ON 하더라도, actual tag 는 ON 되지 않는다.
                enqueue(plan, true)
                pte.Plan.Value === true
                pte.Value === false
                pte.Actual.Value === false

                // actual tag ON 시, pte 전체 ON
                enqueue(actual, true)
                pte.Value === true

                // actual tag 흔들림시, pte 전체도 연동
                (fun () -> enqueue(actual, false))
                |> ShouldFailWithSubstringT<DsException> "Spatial Error:"

                actual.Value === false
                pte.Value === false


            let ``_PortInfoEnd 특이 case 테스트`` =
                let cpu = new Cpu("dummy", new Model())
                let actual = new Tag(cpu, null, "T2_test3", TagType.None, true)

                // Actual 이 ON 인 상태에서의 creation
                let pte = PortInfoEnd.Create(cpu, null, "_PortInfoEnd_test3", actual)

                let wait() = wait(cpu)
                let enqueue(bit, value) =
                    cpu.Enqueue(bit, value)
                    wait()

                cpu.BuildBitDependencies()
                let runSubscription = cpu.Run()

                let plan = pte.Plan
                pte.Value === false
                pte.Plan.Value === false
                pte.Actual.Value === true

                // actual tag ON 상태에서 plan 만 ON 시킬 수 없다.
                (fun () -> enqueue(pte, true))
                |> ShouldFailWithSubstringT<DsException> "Spatial Error:"

                pte.Value === false
                plan.Value === false


                // actual tag OFF 상태에서는 plan OFF 가능
                enqueue(actual, false)
                enqueue(plan, true)
                pte.Plan.Value === true
                pte.Actual.Value === false
                pte.Value === false

                enqueue(pte.Actual, true)
                pte.Value === true

                ()
            ()

