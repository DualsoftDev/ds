namespace UnitTest.Mockup.Engine


open Xunit
open Engine.Core
open Dual.Common
open Xunit.Abstractions
open UnitTest.Engine
open System.Reactive.Disposables
open System
open System.Threading

[<AutoOpen>]
module VirtualParentTestTest =
    type Tests1(output1:ITestOutputHelper) =
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

            [b :> MockupSegmentBase; g; r; vpB;] |> Seq.iter(fun seg -> seg.WireEvent() |> ignore)

            logDebug "====================="
            cpu.PrintAllTags(false);
            logDebug "---------------------"
            cpu.PrintAllTags(true);
            logDebug "====================="

            cpu.BuildBitDependencies()

            let rpexB = vpB.PortR
            let rpexBAnd = rpexB.Plan :?> And       // auto & Latch
            let rpexBAndLatch = rpexBAnd._monitoringBits[1]
            cpu.CollectForwardDependantBits(vpB.PortE).Contains(rpexBAndLatch) === true

            let xxx = cpu.BitsMap["epexB_default"]
            cpu.BitsMap["rt_default_B"] === rtB
            let rpB = b.PortR
            cpu.CollectForwardDependantBits(rtB).Contains(rpB) === true

            let runSubscription = cpu.Run()

            let x = cpu.ForwardDependancyMap[stB]
            let xs = cpu.CollectForwardDependantBits(stB)|> Array.ofSeq
            let gg = cpu.ForwardDependancyMap[g.Going]
            let ggs = cpu.CollectForwardDependantBits(g.Going) |> Array.ofSeq
            vpB.PortS.Value === false

            cpu.Enqueue(auto, true, "최초 auto 시작")
            cpu.Enqueue(stB, true)
            //auto.Value <- true
            //stB.Value <- true
            wait()
            vpB.PortS.Value === true
            ()


        [<Fact>]
        member __.``Vps 실행 test`` () =
            /// 목적 수행 횟수
            let numCycles = 10000

            let cpu = MockUpCpu.create("dummy")
            let wait() = wait(cpu)

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
                            cpu.Enqueue(stB, false, "Turning off 최초 시작 trigger")
                            subscriptionExternalStartOff.Dispose())

            // 목적 cycle 수행 후, auto off 및 시험 종료
            let mutable subscriptionAutoOff = Disposable.Empty
            let mutable testFinished = false
            subscriptionAutoOff <-
                Global.BitChangedSubject
                    .Subscribe(fun bc ->
                        if bc.Bit = b.PortE && b.FinishCount = numCycles then
                            logInfo $"자동 운전 종료"
                            cpu.Enqueue(auto, false, "Auto off")
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
            [b :> MockupSegmentBase; g; r; vpB; vpG; vpR] |> Seq.iter(fun seg -> seg.WireEvent() |> ignore)


            cpu.BuildBitDependencies()
            let runSubscription = cpu.Run()

            assert([vpB; vpG; vpR] |> Seq.forall(fun vp -> vp.GetSegmentStatus() = Status4.Ready));
            [vpB; vpG; vpR] |> Seq.iter(fun vp -> cpu.Enqueue(vp.Ready, true));

            cpu.Enqueue(auto, true, "최초 auto 시작")
            cpu.Enqueue(stB, true, "최초 B 시작")

            wait()
            while not testFinished do
                Thread.Sleep(100)

            logInfo "Test 종료"
