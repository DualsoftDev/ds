namespace UnitTest.Mockup.Engine


open Xunit
open Engine.Core
open Dual.Common
open Xunit.Abstractions
open System.Reactive.Linq
open System.Threading
open UnitTest.Engine
open System.Collections.Concurrent

[<AutoOpen>]
module MockUpClasses =
    type MuSegmentBase(cpu, n, sp, rp, ep) =
        inherit Segment(n)
        member val Cpu:Cpu = cpu
        member val PortS:PortExpressionStart = sp with get, set
        member val PortR:PortExpressionReset = rp with get, set
        member val PortE:PortExpressionEnd = ep with get, set
        member val Going = new Tag(cpu, null, $"{n}_Going")
        member val Ready = new Tag(cpu, null, $"{n}_Ready")
        member x.GetSegmentStatus() =
            match x.PortS.Value, x.PortR.Value, x.PortE.Value with
            | false, false, false -> Status4.Ready  //??
            | true, false, false  -> Status4.Going
            | _, false, true      -> Status4.Finished
            | _, true, _          -> Status4.Homing

    type MuSegment(cpu, n, sp, rp, ep) =
        inherit MuSegmentBase(cpu, n, sp, rp, ep)
        let mutable oldStatus = Status4.Homing

        static member Create(cpu, n) =
            let sp = PortExpressionStart(cpu, null, $"spex{n}_default", null, null)
            let rp = PortExpressionReset(cpu, null, $"rpex{n}_default", null, null)
            let ep = PortExpressionEnd.Create(cpu, null, $"epex{n}", null)
            let seg = MuSegment(cpu, n, sp, rp, ep)
            sp.Segment <- seg
            rp.Segment <- seg
            ep.Segment <- seg
            seg


        member val FinishCount = 0 with get, set

        member x.WireEvent() =
            Global.BitChangedSubject
                .Where(fun bc ->
                    [x.PortS :> IBit; x.PortR; x.PortE] |> Seq.contains(bc.Bit)
                )
                .Subscribe(fun bc ->
                    let newSegmentState = x.GetSegmentStatus()
                    if newSegmentState = oldStatus then
                        logDebug $"\t\tSkipping duplicate status: [{x.Name}] status : {newSegmentState}"
                    else
                        oldStatus <- newSegmentState
                        logDebug $"[{x.Name}] Segment status : {newSegmentState}"

                        match newSegmentState with
                        | Status4.Ready    ->
                            ()
                        | Status4.Going    ->
                            x.Going.Value <- true
                            Thread.Sleep(100)
                            assert(x.GetSegmentStatus() = Status4.Going)
                            x.Going.Value <- false
                            x.PortE.Value <- true
                        | Status4.Finished ->
                            x.FinishCount <- x.FinishCount + 1
                            assert(x.PortE.Value)
                        | Status4.Homing   ->
                            if x.PortE.Value then
                                x.PortE.Value <- false
                                assert(not x.PortE.Value)
                            else
                                logDebug $"\tSkipping [{x.Name}] Segment status : {newSegmentState} : already homing by bit change {bc.Bit}={bc.NewValue}"
                                ()

                            assert(not x.PortE.Value)

                        | _ ->
                            failwith "Unexpected"
                )

    type MuCpu(n) =
        inherit Cpu(n, new Model())
        member val MuQueue = new ConcurrentQueue<BitChange>()

    /// Virtual Parent Segment
    /// endport 는 target child 의 endport 공유
    /// startPort 는 공정 auto
    /// resetPort = { auto &&
    ///     #latch( (Self
    ///                 && #latch(#g(Previous), #r(Self)  <--- reset 조건 1
    ///                 && #latch(#g(Next), #r(Self)),    <--- reset 조건 2 ...
    ///             #r(Self))
    type Vps(target:MuSegment, causalSourceSegments:MuSegment seq, startPort, resetPort) =
        inherit MuSegmentBase(target.Cpu, $"VPS({target.Name})", startPort, resetPort, target.PortE)
        private new(target, causalSourceSegments) = Vps(target, causalSourceSegments, null, null)
        member val Target = target;
        member val PreChildren = causalSourceSegments |> Array.ofSeq

        static member Create(target:MuSegment, auto:IBit, causalSourceSegments:MuSegment seq, resetSourceSegments:MuSegment seq) =
            let cpu = target.Cpu
            let vps = Vps(target, causalSourceSegments)
            let n = vps.Name
            if isNull target.PortE then
                target.PortE <- PortExpressionEnd.Create(cpu, target, $"epex({n})", null)
            let resetPortExpressionPlan =
                let vrp =
                    [|
                        yield auto
                        let set =
                            let andItems =
                                [|
                                    yield target.PortE :> IBit
                                    for rsseg in resetSourceSegments do
                                        yield Latch.Create(cpu, $"InnerResetSourceLatch({rsseg.Name})", rsseg.Going, vps.Ready)
                                |]
                            And(cpu, $"InnerResetSourceAnd_{n}", andItems)
                        yield Latch.Create(cpu, $"ResetLatch({n})", set, vps.Ready)
                    |]
                And(cpu, $"ResetPortExpression({n})", vrp)

            vps.PortR <- PortExpressionReset(cpu, vps, $"Reset_{n}", resetPortExpressionPlan, null)

            vps.PortS <-
                match auto with
                | :? PortExpressionStart as sp -> sp
                | _ -> PortExpressionStart(cpu, vps, $"Start_{n}", auto, null)

            vps

