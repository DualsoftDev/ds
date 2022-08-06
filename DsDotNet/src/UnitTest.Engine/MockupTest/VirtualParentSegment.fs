namespace UnitTest.Mockup.Engine


open Engine.Core
open Dual.Common
open System.Linq
open Engine.Runner
open System.Net


[<AutoOpen>]
module VirtualParentSegment =

    /// Virtual Parent Segment
    /// endport 는 target child 의 endport 공유
    /// startPort 는 공정 auto
    /// resetPort = { auto &&
    ///     #latch( (Self
    ///                 && #latch(#g(Previous), #r(Self)  <--- reset 조건 1
    ///                 && #latch(#g(Next), #r(Self)),    <--- reset 조건 2 ...
    ///             #r(Self))
    type Vps(name, target:MuSegment, causalSourceSegments:MuSegment seq,
        startPort, resetPort, endPort,
        goingTag, readyTag,
        targetStartTag, targetResetTag               // target child 의 start port 에 가상 부모가 시작시킬 수 있는 start tag 추가 (targetStartTag)
    ) =
        inherit MuSegmentBase(target.Cpu, name, startPort, resetPort, endPort, goingTag, readyTag)
        let cpu = target.Cpu
        let mutable oldStatus = Status4.Homing
        let mutable prevChildrenChecked:bool option = None

        //private new(target, causalSourceSegments) = Vps(target, causalSourceSegments, null, null)
        member val Target = target;
        member val PreChildren = causalSourceSegments |> Array.ofSeq
        member val TargetStartTag = targetStartTag with get
        member val TargetResetTag = targetResetTag with get

        static member Create(target:MuSegment, auto:IBit, (targetStartTag:IBit, targetResetTag:IBit), causalSourceSegments:MuSegment seq, resetSourceSegments:MuSegment seq) =
            let cpu = target.Cpu
            let n = $"VPS_{target.Name}"

            let readyTag = new Tag(cpu, null, $"{n}_Ready")

            let ep = PortExpressionEnd.Create(cpu, null, $"End_{n}", null)
            let resetPortExpressionPlan =
                let vrp =
                    [|
                        yield auto
                        let set =
                            let andItems = [|
                                yield target.PortE :> IBit
                                for rsseg in resetSourceSegments do
                                    let going = rsseg.Going//And(cpu, $"InnerResetSourceLatchAnd_{n}", ep, rsseg.Going)
                                    yield Latch.Create(cpu, $"InnerResetSourceLatch_{n}_{rsseg.Name}", going, readyTag)
                            |]
                            And(cpu, $"InnerResetSourceAnd_{n}", andItems)
                        yield Latch.Create(cpu, $"ResetLatch_{n}", set, readyTag)
                    |]
                And(cpu, $"ResetPortExpression_{n}", vrp)

            let sp =
                match auto with
                | :? PortExpressionStart as sp -> sp
                | _ -> PortExpressionStart(cpu, null, $"Start_{n}", auto, null)
            let rp = PortExpressionReset(cpu, null, $"Reset_{n}", resetPortExpressionPlan, null)
            let vps = Vps(n, target, causalSourceSegments, sp, rp, ep, null, readyTag, targetStartTag, targetResetTag)
            sp.Segment <- vps
            rp.Segment <- vps
            ep.Segment <- vps

            vps

        override x.WireEvent() =
            let prevChildrenEndPorts = x.PreChildren |> Array.map(fun seg -> seg.PortE)
            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit :?> Bit
                    let on = bc.Bit.Value
                    let allPrevChildrenFinished = prevChildrenEndPorts.All(fun ep -> ep.Value)
                    let notiPrevChildFinish =
                        on
                            && bc.Bit :? PortExpressionEnd
                            && prevChildrenEndPorts |> Seq.contains(bc.Bit :?> PortExpressionEnd)
                    let notiVpsPortChange = [x.PortS :> IBit; x.PortR; x.PortE] |> Seq.contains(bc.Bit)
                    let notiTargetEndPortChange = bc.Bit = x.Target.PortE
                    let newSegmentState = x.GetSegmentStatus()

                    if notiPrevChildFinish || notiVpsPortChange || notiTargetEndPortChange then
                        noop()

                    if notiPrevChildFinish then
                        let vpsStatus = x.GetSegmentStatus()
                        let targetChildStatus = x.Target.GetSegmentStatus()
                        if allPrevChildrenFinished then
                            logDebug $"[{x.Name}]{newSegmentState} - All prev child [{x.PreChildren[0].Name}] finish detected"
                            assert (prevChildrenChecked <> Some true)
                            prevChildrenChecked <- Some true
                            if x.Name = "VPS_B" then
                                noop()
                        match prevChildrenChecked, vpsStatus, targetChildStatus with
                        | None, Status4.Going, Status4.Ready
                        | Some true, Status4.Going, Status4.Ready -> // 사전 조건 완료, target child 수행
                            logDebug $"[{x.Name}] - Executing child.."
                            cpu.Enqueue(targetStartTag, true)
                        | _ ->
                            ()

                    if notiTargetEndPortChange then
                        if x.Going.Value && newSegmentState <> Status4.Going then
                            cpu.Enqueue(x.Going, false)
                        if x.Ready.Value && newSegmentState <> Status4.Ready then
                            cpu.Enqueue(x.Ready, false)

                        match newSegmentState, on with
                        | Status4.Finished, true ->
                            assert(false)
                        | Status4.Going, true ->
                            if x.Name = "VPS_B" then
                                noop()
                            cpu.Enqueue(targetStartTag, false)
                            cpu.Enqueue(x.Going, false)
                            cpu.Enqueue(x.PortE, true)

                        | Status4.Ready, false ->
                            assert(false)
                        | Status4.Homing, false ->
                            assert(x.Going.Value = false)
                            cpu.Enqueue(targetResetTag, false)
                            cpu.Enqueue(x.Ready, true)
                            cpu.Enqueue(x.PortE, false)
                        | _ ->
                            failwithlog $"Unknown: [{x.Name}]{newSegmentState}: Target endport => {x.Target.Name}={on}"


                    if notiVpsPortChange then
                        if newSegmentState = oldStatus then
                            logDebug $"\t\tVPS Skipping duplicate status: [{x.Name}] status : {newSegmentState}"
                        else
                            oldStatus <- newSegmentState
                            logDebug $"[{x.Name}] Segment status : {newSegmentState} by {bit.Name}={bit.Value}"

                            match newSegmentState with
                            | Status4.Ready    ->
                                ()
                            | Status4.Going    ->
                                if x.Name = "VPS_B" then
                                    noop()
                                let targetChildStatus = x.Target.GetSegmentStatus()
                                cpu.Enqueue(x.Going, true)

                                assert(targetChildStatus = Status4.Ready)
                                if allPrevChildrenFinished then
                                    assert(prevChildrenChecked <> Some false)
                                    prevChildrenChecked <- Some false
                                    cpu.Enqueue(targetStartTag, true)

                                //assert(x.GetSegmentStatus() = Status4.Going)
                                //cpu.Enqueue(x.PortE, true)
                                //cpu.Enqueue(x.Going, false)
                                ()
                            | Status4.Finished ->
                                if x.Name = "VPS_B" then
                                    noop()
                                cpu.Enqueue(targetStartTag, false)
                                //cpu.Enqueue(x.PortE, true)
                                cpu.Enqueue(x.Going, false)
                                x.FinishCount <- x.FinishCount + 1
                                assert(x.PortE.Value)
                                assert(x.PortR.Value = false)

                                // self reset
                                cpu.Enqueue(x.PortR, true)
                            | Status4.Homing   ->
                                //assert(prevChildrenChecked <> Some false)
                                if prevChildrenChecked = Some false then
                                    logWarn $"Need previous children finish check on {x.Name}?"
                                prevChildrenChecked <- None
                                assert(x.Target.GetSegmentStatus() = Status4.Finished)
                                assert(x.Target.PortE.Value)
                                cpu.Enqueue(targetResetTag, true)
                                //cpu.Enqueue(x.PortE, false)

                                //assert(not x.PortE.Value)

                            | _ ->
                                failwith "Unexpected"



                        ()
                )
