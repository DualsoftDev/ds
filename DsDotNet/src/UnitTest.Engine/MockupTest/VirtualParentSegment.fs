namespace UnitTest.Mockup.Engine


open Engine.Core
open Dual.Common
open System.Linq
open System.Reactive.Linq
open System.Threading


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
        startPort, resetPort,
        goingTag, readyTag,
        targetStartTag, targetResetTag               // target child 의 start port 에 가상 부모가 시작시킬 수 있는 start tag 추가 (targetStartTag)
    ) =
        inherit MuSegmentBase(target.Cpu, name, startPort, resetPort, target.PortE, goingTag, readyTag)
        let cpu = target.Cpu
        let mutable oldStatus = Status4.Homing

        //private new(target, causalSourceSegments) = Vps(target, causalSourceSegments, null, null)
        member val Target = target;
        member val PreChildren = causalSourceSegments |> Array.ofSeq
        member val TargetStartTag = targetStartTag with get
        member val TargetResetTag = targetResetTag with get

        static member Create(target:MuSegment, auto:IBit, (targetStartTag:IBit, targetResetTag:IBit), causalSourceSegments:MuSegment seq, resetSourceSegments:MuSegment seq) =
            let cpu = target.Cpu
            let n = $"VPS_{target.Name}"

            let readyTag = new Tag(cpu, null, $"{n}_Ready")

            let resetPortExpressionPlan =
                let vrp =
                    [|
                        yield auto
                        let set =
                            let andItems =
                                [|
                                    yield target.PortE :> IBit
                                    for rsseg in resetSourceSegments do
                                        yield Latch.Create(cpu, $"InnerResetSourceLatch_{n}_{rsseg.Name}", rsseg.Going, readyTag)
                                |]
                            And(cpu, $"InnerResetSourceAnd_{n}", andItems)
                        yield Latch.Create(cpu, $"ResetLatch_{n}", set, readyTag)
                    |]
                And(cpu, $"ResetPortExpression_{n}", vrp)

            let rp = PortExpressionReset(cpu, null, $"Reset_{n}", resetPortExpressionPlan, null)
            let sp =
                match auto with
                | :? PortExpressionStart as sp -> sp
                | _ -> PortExpressionStart(cpu, null, $"Start_{n}", auto, null)
            let vps = Vps(n, target, causalSourceSegments, sp, rp, null, readyTag, targetStartTag, targetResetTag)
            sp.Segment <- vps
            rp.Segment <- vps

            vps

        override x.WireEvent() =
            let prevChildrenEndPorts = x.PreChildren |> Array.map(fun seg -> seg.PortE)
            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let bit = bc.Bit :?> Bit
                    let on = bc.Bit.Value
                    let notiPrevChildFinish =
                        on
                            && bc.Bit :? PortExpressionEnd
                            && prevChildrenEndPorts |> Seq.contains(bc.Bit :?> PortExpressionEnd)
                    let notiVpsPortChange = [x.PortS :> IBit; x.PortR; x.PortE] |> Seq.contains(bc.Bit)
                    let notiTargetFinish = on && bc.Bit = x.Target.PortE

                    if notiTargetFinish then
                        logDebug $"VPS[{x.Name}] - Turning off child reset port{x.Target.Name}.."
                        cpu.Enqueue(targetResetTag, false)
                        cpu.Enqueue(x.PortE, true)
                        cpu.Enqueue(x.Going, false)

                    elif notiPrevChildFinish then
                        let allPrevChildrenFinished = prevChildrenEndPorts.All(fun ep -> ep.Value)
                        let vpsStatus = x.GetSegmentStatus()
                        let targetChildStatus = x.Target.GetSegmentStatus()
                        if allPrevChildrenFinished then
                            logDebug $"VPS[{x.Name}] - All prev child [{x.PreChildren[0].Name}] finish detected"
                        match allPrevChildrenFinished, vpsStatus, targetChildStatus with
                        | true, Status4.Going, Status4.Ready -> // 사전 조건 완료, target child 수행
                            logDebug $"VPS[{x.Name}] - Executing child.."
                            cpu.Enqueue(targetStartTag, true)
                        | _ ->
                            ()

                    elif notiVpsPortChange then
                        let newSegmentState = x.GetSegmentStatus()
                        if newSegmentState = oldStatus then
                            logDebug $"\t\tVPS Skipping duplicate status: [{x.Name}] status : {newSegmentState}"
                        else
                            oldStatus <- newSegmentState
                            logDebug $"[{x.Name}] Segment status : {newSegmentState} by {bit.Name}={bit.Value}"

                            match newSegmentState with
                            | Status4.Ready    ->
                                ()
                            | Status4.Going    ->
                                let targetChildStatus = x.Target.GetSegmentStatus()
                                cpu.Enqueue(x.Going, true)
                                //assert(x.GetSegmentStatus() = Status4.Going)
                                //cpu.Enqueue(x.PortE, true)
                                //cpu.Enqueue(x.Going, false)
                                ()
                            | Status4.Finished ->
                                cpu.Enqueue(targetStartTag, false)
                                x.FinishCount <- x.FinishCount + 1
                                assert(x.PortE.Value)
                            | Status4.Homing   ->
                                assert(x.Target.GetSegmentStatus() = Status4.Finished)
                                assert(x.Target.PortE.Value)
                                cpu.Enqueue(targetResetTag, true)
                                if x.PortE.Value then
                                    cpu.Enqueue(x.PortE, false)
                                    assert(not x.PortE.Value)
                                else
                                    logDebug $"\tSkipping [{x.Name}] Segment status : {newSegmentState} : already homing by bit change {bc.Bit.GetName()}={bc.NewValue}"
                                    ()

                                assert(not x.PortE.Value)

                            | _ ->
                                failwith "Unexpected"



                        ()
                )
