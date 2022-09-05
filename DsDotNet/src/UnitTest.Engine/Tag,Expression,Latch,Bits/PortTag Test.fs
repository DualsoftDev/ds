namespace UnitTest.Engine


open Xunit
open Engine.Core
open Engine.Common.FS
open Engine.Runner
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
                let plan = new TagE(cpu, null, "T1_test1")
                let actual = new TagE(cpu, null, "T2_test1")
                let pts = new PortInfoStart(cpu, null, "PortInfoStart", plan, actual)


                let wait() = wait(cpu)
                let enqueue(bit, value) = cpu.Enqueue(bit, value)

                cpu.BuildBitDependencies()
                let runSubscription = cpu.Run()

                task {
                    pts.Value === false
                    pts.Plan.Value === false
                    pts.Actual.Value === false

                    //// Expression 으로, 값을 설정할 수 없어야 한다.
                    //(fun () -> enqueue(pts, true))
                    //|> ShouldFail

                    do! enqueue(pts, true)
                    pts.Plan.Value === true
                    pts.Value === true
                    pts.Actual.Value === true

                    // plan OFF 시, actual tag 도 OFF 되어야 한다.
                    do! enqueue(plan, false)
                    pts.Plan.Value === false
                    pts.Value === false
                    pts.Actual.Value === false


                    // pts plan tag ON 시, pts 도 ON 되어야 한다.
                    do! enqueue(plan, true)
                    pts.Plan.Value === true
                    pts.Value === true
                    pts.Actual.Value === true
                } |> Async.AwaitTask |> Async.RunSynchronously

            let ``_PortInfoEnd Normal 테스트`` =
                let cpu = new Cpu("dummy", new Model())
                let actual = new TagE(cpu, null, "T2_test2")

                let pte = PortInfoEnd.Create(cpu, null, "_PortInfoEnd_test2", actual)

                let wait() = wait(cpu)
                let enqueue(bit, value) = cpu.Enqueue(bit, value)

                cpu.BuildBitDependencies()
                let runSubscription = cpu.Run()

                task {
                    let plan = pte.Plan
                    pte.Value === false
                    pte.Plan.Value === false
                    pte.Actual.Value === false

                    // pte 전체 ON 하더라도, actual tag 는 ON 되지 않는다.
                    do! enqueue(plan, true)
                    pte.Plan.Value === true
                    pte.Value === false
                    pte.Actual.Value === false

                    // actual tag ON 시, pte 전체 ON
                    do! enqueue(actual, true)
                    pte.Value === true

                    // actual tag 흔들림시, pte 전체도 연동
                    (fun () -> enqueue(actual, false).Wait())
                    |> ShouldFailWithSubstringT<DsException> "Spatial Error:"

                    actual.Value === false
                    pte.Value === false
                } |> Async.AwaitTask |> Async.RunSynchronously


            let ``_PortInfoEnd 특이 case 테스트`` =
                let cpu = new Cpu("dummy", new Model())
                let actual = new TagE(cpu, null, "T2_test3", TagType.None, true)

                // Actual 이 ON 인 상태에서의 creation
                let pte = PortInfoEnd.Create(cpu, null, "_PortInfoEnd_test3", actual)

                let wait() = wait(cpu)
                let enqueue(bit, value) = cpu.Enqueue(bit, value)

                cpu.BuildBitDependencies()
                let runSubscription = cpu.Run()

                task {
                    let plan = pte.Plan
                    pte.Value === false
                    pte.Plan.Value === false
                    pte.Actual.Value === true

                    // actual tag ON 상태에서 plan 만 ON 시킬 수 없다.
                    (fun () -> enqueue(pte, true).Wait())
                    |> ShouldFailWithSubstringT<DsException> "Spatial Error:"

                    pte.Value === false
                    plan.Value === false


                    // actual tag OFF 상태에서는 plan OFF 가능
                    do! enqueue(actual, false)
                    do! enqueue(plan, true)
                    pte.Plan.Value === true
                    pte.Actual.Value === false
                    pte.Value === false

                    do! enqueue(pte.Actual, true)
                    pte.Value === true
                } |> Async.AwaitTask |> Async.RunSynchronously



                ()
            ()

