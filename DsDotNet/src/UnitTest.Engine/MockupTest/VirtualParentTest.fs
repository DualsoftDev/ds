namespace UnitTest.Mockup.Engine


open Xunit
open Engine.Core
open Engine.Runner
open Dual.Common
open Xunit.Abstractions
open UnitTest.Engine
open System.Reactive.Disposables
open System
open System.Threading

[<AutoOpen>]
module VirtualParentTestTest =
    type Tests1(output1:ITestOutputHelper) =

        let mutable testFinished = false
        let prepare(cpu:Cpu, writer:ChangeWriter, numCycles) =

            let b, (stB, rtB) = MockupSegment.CreateWithDefaultTags(cpu, "B")
            let g, (stG, rtG) = MockupSegment.CreateWithDefaultTags(cpu, "G")
            let r, (stR, rtR) = MockupSegment.CreateWithDefaultTags(cpu, "R")

            let auto = new Tag(cpu, null, "auto")

            //let vpB = Vps.Create(b, auto, (stB, rtB), [g], [r])
            let vpB = Vps.Create(b, auto, (stB, rtB), [g], [g; r])
            let vpG = Vps.Create(g, auto, (stG, rtG), [r], [b])
            let vpR = Vps.Create(r, auto, (stR, rtR), [b], [g])
            logDebug $"B Start:{vpB.PortS.ToText()}";
            logDebug $"B Reset:{vpB.PortR.ToText()}";
            logDebug $"B End:{vpB.PortE.ToText()}";


            logDebug "====================="
            cpu.PrintAllTags(false);
            logDebug "---------------------"
            cpu.PrintAllTags(true);
            logDebug "====================="



            // 외부 시작 (start B) off
            let mutable subscriptionExternalStartOff = Disposable.Empty
            subscriptionExternalStartOff <-
                Global.BitChangedSubject
                    .Subscribe(fun bc ->
                        if bc.Bit = b.PortE then
                            assert b.PortE.Value
                            writer(stB, false, "Turning off 최초 시작 trigger")
                            subscriptionExternalStartOff.Dispose())

            // 목적 cycle 수행 후, auto off 및 시험 종료
            let mutable subscriptionAutoOff = Disposable.Empty
            subscriptionAutoOff <-
                Global.BitChangedSubject
                    .Subscribe(fun bc ->
                        if bc.Bit = g.PortE && g.FinishCount = numCycles then
                            logInfo $"자동 운전 종료"
                            writer(auto, false, "Auto off")
                            cpu.Running <- false
                            // 수행 횟수 확인
                            async {
                                while cpu.ProcessingQueue do
                                    do! Async.Sleep(10)
                                [b.FinishCount; g.FinishCount; r.FinishCount] |> List.forall((=) numCycles) |> ShouldBeTrue
                                logInfo $"최종 결과 확인 완료!"
                                testFinished <- true
                            } |> Async.Start
                            subscriptionAutoOff.Dispose())


            // bit 변경 및 수행 횟수 logging
            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit
                    let cause = if isNull bc.CauseRepr then "" else $" caused by [{bc.CauseRepr}]"
                    logDebug $"\tBit changed: [{bit.GetName()}] = {bc.NewValue}{cause}"

                    match bit with
                    | :? PortInfoEnd as portE ->
                        let seg = portE.Segment :?> MockupSegmentBase
                        if bit = g.PortE && g.PortE.Value then
                            if seg.FinishCount % 10 = 0 then
                                logDebug $"COUNTER: B={b.FinishCount}, G={g.FinishCount}, R={r.FinishCount}"
                    | _ ->
                        ()
                ) |> ignore

            // 각 segment 별 event handling 등록
            [b :> MockupSegmentBase; g; r; vpB; vpG; vpR] |> Seq.iter(fun seg -> seg.WireEvent(writer) |> ignore)

            assert([vpB; vpG; vpR] |> Seq.forall(fun vp -> vp.Status = Status4.Ready));

            cpu.BuildBitDependencies()
            let runSubscription = cpu.Run()

            vpB, vpG, vpR, auto, stB


        interface IClassFixture<Fixtures.DemoFixture>

        [<Fact>]
        member __.``Vps 생성 test`` () =
            let cpu = MockUpCpu.create("dummy")
            let wait() = wait(cpu)

            let b, (stB, rtB) = MockupSegment.CreateWithDefaultTags(cpu, "B")
            let g, (stG, rtG) = MockupSegment.CreateWithDefaultTags(cpu, "G")
            let r, (stR, rtR) = MockupSegment.CreateWithDefaultTags(cpu, "R")

            let auto = new Tag(cpu, null, "auto")

            let vpB = Vps.Create(b, auto, (stB, rtB), [g], [g; r])


            let mutable subscription = Disposable.Empty
            subscription <-
                Global.BitChangedSubject
                    .Subscribe(fun bc ->
                        if bc.Bit = b.PortE (*&& b.PortE.Value *)then
                            logDebug "Turning off 최초 시작 trigger"
                            cpu.Enqueue(stB, false)
                            subscription.Dispose())

            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit
                    let cause = if isNull bc.CauseRepr then "" else $" caused by [{bc.CauseRepr}]"
                    logDebug $"\tBit changed: [{bit.GetName()}] = {bc.NewValue}{cause}") |> ignore

            [b :> MockupSegmentBase; g; r; vpB;] |> Seq.iter(fun seg -> seg.WireEvent(cpu.Enqueue) |> ignore)

            logDebug "====================="
            cpu.PrintAllTags(false);
            logDebug "---------------------"
            cpu.PrintAllTags(true);
            logDebug "====================="

            cpu.BuildBitDependencies()

            //let rpexB = vpB.PortR
            //let rpexBAnd = rpexB.Plan :?> And       // auto & Latch
            //let rpexBAndLatch = rpexBAnd._monitoringBits[2]
            //cpu.CollectForwardDependantBits(vpB.PortE).Contains(rpexBAndLatch) === true

            //let xxx = cpu.BitsMap["epexB_default"]
            //cpu.BitsMap["rt_default_B"] === rtB
            //let rpB = b.PortR
            //cpu.CollectForwardDependantBits(rtB).Contains(rpB) === true

            //let runSubscription = cpu.Run()

            //let x = cpu.ForwardDependancyMap[stB]
            //let xs = cpu.CollectForwardDependantBits(stB)|> Array.ofSeq
            //let gg = cpu.ForwardDependancyMap[g.Going]
            //let ggs = cpu.CollectForwardDependantBits(g.Going) |> Array.ofSeq
            //vpB.PortS.Value === false

            //cpu.Enqueue(auto, true, "최초 auto 시작")
            //cpu.Enqueue(stB, true)
            ////auto.Value <- true
            ////stB.Value <- true
            //wait()
            //vpB.PortS.Value === true
            ()


        // 가상 부모가 child start 시키려 할 때, child 가 준비되지 않은 경우에 한해 새로 생성된 thread 상에서 child 기다렸다가 시킴.
        [<Fact>]
        member __.``Single thread w/ Queueing : OK`` () =
            let cpu = MockUpCpu.create("dummy")
            let vpB, vpG, vpR, auto, stB = prepare(cpu, cpu.Enqueue, 100)

            [vpB; vpG; vpR] |> Seq.iter(fun vp -> cpu.PostChange(vp.Ready, true, null));

            cpu.PostChange(auto, true, "최초 auto 시작")
            cpu.PostChange(stB, true, "최초 B 시작")

            wait(cpu)
            while not testFinished do
                Thread.Sleep(100)

            logInfo "Test 종료"



        [<Fact>]
        member __.``FAIL: Single thread w/o Queue => Stack overflow`` () =
            let cpu = MockUpCpu.create("dummy")
            let vpB, vpG, vpR, auto, stB = prepare(cpu, cpu.SendChange, 100)

            [vpB; vpG; vpR] |> Seq.iter(fun vp -> cpu.SendChange(vp.Ready, true, null));

            try
                cpu.SendChange(auto, true, "최초 auto 시작")
                cpu.SendChange(stB, true, "최초 B 시작")
            with exn ->
                logError $"Exception: {exn}"

            wait(cpu)
            while not testFinished do
                Thread.Sleep(100)

            logInfo "Test 종료"


        // test 는 fail 이지만, 생성된 thread 마지막 check 누락으로 인한 것임.  논리적으로는 OK
        [<Fact>]
        member __.``Multithread on PortEnd w/o Queue : OK`` () =
            MockupSegmentBase.WithThreadOnPortEnd <- true

            let cpu = MockUpCpu.create("dummy")
            let vpB, vpG, vpR, auto, stB = prepare(cpu, cpu.SendChange, 1000)

            [vpB; vpG; vpR] |> Seq.iter(fun vp -> cpu.SendChange(vp.Ready, true, null));

            cpu.SendChange(auto, true, "최초 auto 시작")
            cpu.SendChange(stB, true, "최초 B 시작")

            wait(cpu)
            while not testFinished do
                Thread.Sleep(100)

            logInfo "Test 종료"

        [<Fact>]
        member __.``FAIL: Multithread on PortReset w/o Queue => Stack overflow`` () =
            MockupSegmentBase.WithThreadOnPortReset <- true

            let cpu = MockUpCpu.create("dummy")
            let vpB, vpG, vpR, auto, stB = prepare(cpu, cpu.SendChange, 1000)

            [vpB; vpG; vpR] |> Seq.iter(fun vp -> cpu.SendChange(vp.Ready, true, null));

            cpu.SendChange(auto, true, "최초 auto 시작")
            cpu.SendChange(stB, true, "최초 B 시작")

            wait(cpu)
            while not testFinished do
                Thread.Sleep(100)

            logInfo "Test 종료"


        /// target child Going 중에 parent reset 받음.
        [<Fact>]
        member __.``FAIL: Multithread on Port{End, Reset} w/o Queue`` () =
            MockupSegmentBase.WithThreadOnPortEnd <- true
            MockupSegmentBase.WithThreadOnPortReset <- true

            let cpu = MockUpCpu.create("dummy")
            let vpB, vpG, vpR, auto, stB = prepare(cpu, cpu.SendChange, 1000)

            [vpB; vpG; vpR] |> Seq.iter(fun vp -> cpu.SendChange(vp.Ready, true, null));

            cpu.SendChange(auto, true, "최초 auto 시작")
            cpu.SendChange(stB, true, "최초 B 시작")

            (* 실패 원인:
                - dead lock (block)
                - Something bad happend?  trying to reset child while R=Going
            *)
            
            wait(cpu)
            while not testFinished do
                Thread.Sleep(100)

            logInfo "Test 종료"


        [<Fact>]
        member __.``Multithread on PortEnd with Queue : OK`` () =
            MockupSegmentBase.WithThreadOnPortEnd <- true

            let cpu = MockUpCpu.create("dummy")
            let vpB, vpG, vpR, auto, stB = prepare(cpu, cpu.Enqueue, 1000)

            [vpB; vpG; vpR] |> Seq.iter(fun vp -> cpu.Enqueue(vp.Ready, true, null));

            cpu.Enqueue(auto, true, "최초 auto 시작")
            cpu.Enqueue(stB, true, "최초 B 시작")

            wait(cpu)
            while not testFinished do
                Thread.Sleep(100)

            logInfo "Test 종료"



