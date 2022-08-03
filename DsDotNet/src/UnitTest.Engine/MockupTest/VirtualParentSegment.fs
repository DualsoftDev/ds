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
    type Vps(target:MuSegment, causalSourceSegments:MuSegment seq, startPort, resetPort) =
        inherit MuSegmentBase(target.Cpu, $"VPS({target.Name})", startPort, resetPort, target.PortE)
        let cpu = target.Cpu
        let mutable oldStatus = Status4.Homing
        let targetStartTag = Tag(target.Cpu, target, $"Start_{target.Name}_FromVPS")
        do
            // target child 의 start port 에 가상 부모가 시작시킬 수 있는 start tag 추가 (targetStartTag)
            target.PortS.Plan <- Or(cpu, $"OR_SP_{target.Name}", [| targetStartTag :> IBit; target.PortS.Plan|])
            target.PortS.ReSubscribe()

        private new(target, causalSourceSegments) = Vps(target, causalSourceSegments, null, null)
        member val Target = target;
        member val PreChildren = causalSourceSegments |> Array.ofSeq
        member val TargetStartTag = targetStartTag with get

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

        member x.WireEvent() =
            let prevChildrenEndPorts = x.PreChildren |> Array.map(fun seg -> seg.PortE)
            Global.BitChangedSubject
                .Subscribe(fun bc ->
                    let notiPrevChildFinish =
                        bc.Bit :? PortExpressionEnd
                            && prevChildrenEndPorts |> Seq.contains(bc.Bit :?> PortExpressionEnd)
                    let notiVpsPortChange = [x.PortS :> IBit; x.PortR; x.PortE] |> Seq.contains(bc.Bit)

                    if notiPrevChildFinish then
                        let allPrevChildrenFinished = prevChildrenEndPorts.All(fun ep -> ep.Value)
                        let vpsStatus = x.GetSegmentStatus()
                        let targetChildStatus = x.Target.GetSegmentStatus()
                        match allPrevChildrenFinished, vpsStatus, targetChildStatus with
                        | true, Status4.Going, Status4.Ready -> // 사전 조건 완료, target child 수행
                            logDebug $"VPS[{x.Name}] - Executing child.."
                            targetStartTag.Value <- true
                        | _ ->
                            ()

                        if allPrevChildrenFinished then
                            ()
                    elif notiVpsPortChange then
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
                                let targetChildStatus = x.Target.GetSegmentStatus()
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



                        ()
                ) |> ignore
