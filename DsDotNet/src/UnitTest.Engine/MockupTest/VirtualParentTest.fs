namespace UnitTest.Mockup.Engine


open Xunit
open Engine.Core
open Dual.Common
open Xunit.Abstractions
open System.Threading
open UnitTest.Engine
open System.Reactive.Disposables

[<AutoOpen>]
module VirtualParentTestTest =
    type Tests1(output1:ITestOutputHelper) =
        interface IClassFixture<Fixtures.DemoFixture>

        [<Fact>]
        member __.``Vps 생성 test`` () =
            let cpu = new MuCpu("dummy")

            let b, (stB, rtB) = MuSegment.CreateWithDefaultTags(cpu, "B")
            let g, (stG, rtG) = MuSegment.CreateWithDefaultTags(cpu, "G")
            let r, (stR, rtR) = MuSegment.CreateWithDefaultTags(cpu, "R")

            //b.PortR.Plan <- Latch(cpu, "rlB", rtB, Not(b.PortE))
            //g.PortR.Plan <- Latch(cpu, "rlG", rtG, Not(g.PortE))
            //r.PortR.Plan <- Latch(cpu, "rlR", rtR, Not(r.PortE))

            let auto = new Tag(cpu, null, "auto")

            let vpB = Vps.Create(b, auto, (stB, rtB), [g], [g; r])


            let mutable subscription = Disposable.Empty
            subscription <-
                Global.BitChangedSubject
                    .Subscribe(fun bc ->
                        if bc.Bit = b.PortE (*&& b.PortE.Value *)then
                            logDebug "Turning off 최초 시작 trigger"
                            stB.Value <- false
                            subscription.Dispose())

            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit
                    let cause = if isNull bc.Cause then "" else $" caused by [{bc.Cause.GetName()}={bc.Cause.Value}]"
                    logDebug $"\tBit changed: [{bit.GetName()}] = {bc.NewValue}{cause}") |> ignore

            [b :> MuSegmentBase; g; r; vpB;] |> Seq.iter(fun seg -> seg.WireEvent() |> ignore)



            vpB.PortS.Value === false
            auto.Value <- true
            stB.Value <- true
            wait()
            vpB.PortS.Value === true
            ()


        [<Fact>]
        member __.``Vps 실행 test`` () =
            let cpu = new MuCpu("dummy")

            let b, (stB, rtB) = MuSegment.CreateWithDefaultTags(cpu, "B")
            let g, (stG, rtG) = MuSegment.CreateWithDefaultTags(cpu, "G")
            let r, (stR, rtR) = MuSegment.CreateWithDefaultTags(cpu, "R")

            //b.PortR.Plan <- Latch(cpu, "rlB", rtB, Not(b.PortE))
            //g.PortR.Plan <- Latch(cpu, "rlG", rtG, Not(g.PortE))
            //r.PortR.Plan <- Latch(cpu, "rlR", rtR, Not(r.PortE))

            let auto = new Tag(cpu, null, "auto")

            //let vpB = Vps.Create(b, auto, (stB, rtB), [g], [g; r])
            let vpB = Vps.Create(b, auto, (stB, rtB), [g], [r])
            logDebug $"B Start:{vpB.PortS.ToText()}";
            logDebug $"B Reset:{vpB.PortR.ToText()}";
            logDebug $"B End:{vpB.PortE.ToText()}";
            let vpG = Vps.Create(g, auto, (stG, rtG), [r], [b])
            let vpR = Vps.Create(r, auto, (stR, rtR), [b], [g])


            //logDebug "====================="
            //cpu.PrintAllTags(false);
            //logDebug "---------------------"
            //cpu.PrintAllTags(true);
            //logDebug "====================="



            let mutable subscription = Disposable.Empty
            subscription <-
                Global.BitChangedSubject
                    .Subscribe(fun bc ->
                        if bc.Bit = b.PortE (*&& b.PortE.Value *)then
                            logDebug "Turning off 최초 시작 trigger"
                            stB.Value <- false
                            subscription.Dispose())

            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit
                    let cause = if isNull bc.Cause then "" else $" caused by [{bc.Cause.GetName()}={bc.Cause.Value}]"
                    logDebug $"\tBit changed: [{bit.GetName()}] = {bc.NewValue}{cause}") |> ignore

            [b :> MuSegmentBase; g; r; vpB; vpG; vpR] |> Seq.iter(fun seg -> seg.WireEvent() |> ignore)


            vpB.PortS.Value === false
            auto.Value <- true
            stB.Value <- true
            wait()
            vpB.PortS.Value === true

            ////auto.Value <- true
            //b.Going.Value <- true
            ////stB.Value <- true
            //wait()
            ////vpB.PortS.Value === true

            ()
