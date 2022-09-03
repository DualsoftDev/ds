namespace Engine.Runner

open System
open System.Linq
open System.Reactive.Linq
open System.Collections.Generic

open Engine.Common.FS
open Engine.Core
open Engine.Common
open System.Collections.Concurrent


[<AutoOpen>]
module internal SegmentRGFHModule =
    [<AutoOpen>]
    module Origin =
        let getChildrenOrigin (seg:SegmentBase) =
            let seg = seg :?> Segment
            seg.ChildrenOrigin.Cast<Child>().ToArray()
        let getOutofOriginChildren (seg:SegmentBase) =
            getChildrenOrigin(seg)
                .Where(fun ch -> ch.TagsEnd.Any(fun t -> not t.Value))
                .ToArray()
        let isChildrenOrigin = getOutofOriginChildren >> Seq.isEmpty

    let verifyM msg x = if not x then failwithlog $"{msg}"
    let runChildren (children:Child seq, writer:ChangeWriter) =
        task {
            assert(children.Distinct().Count() = children.Count())
            //for child in children do
            //    // todo
            //    //assert(not child.Status.HasValue || child.Status = Nullable Status4.Ready)
            //    child.Status <- Status4.Going

            for child in children do
                assert(not child.IsFlipped && (not child.Status.HasValue || child.Status.Value.IsOneOf(Status4.Ready, Status4.Finished)))
                logInfo $"Progress: Child {child.QualifiedName} starting.."

                for st in child.TagsStart do
                    let before = fun () ->
                        child.Status <- Status4.Going
                        Global.ChildStatusChangedSubject.OnNext(ChildStatusChange(child, Status4.Going))

                    do! writer(BitChange(st, true, "Starting child", BeforeAction = before))
        }

    /// Segment 별로 Going 중에 child 의 종료 모니터링.  segment 가 Going 이 아니게 되면, dispose
    let goingSubscriptions = ConcurrentDictionary<SegmentBase, IDisposable>()
    let homingSubscriptions = ConcurrentDictionary<SegmentBase, IDisposable>()
    /// 원위치가 맞지 않은 상태에서 Going 시, 원위치부터 맞추기 위한 작업
    let originatingSubscriptions = ConcurrentDictionary<SegmentBase, IDisposable>()

    let stopMonitor (subscriptions:ConcurrentDictionary<SegmentBase, IDisposable>) (seg:SegmentBase) =
        if subscriptions.ContainsKey(seg) then
            match subscriptions.TryRemove(seg) with
            | true, subs ->
                subs.Dispose()
            | _ ->
                failwithlog $"Failed to remove subscription."

    let stopMonitorGoing (seg:SegmentBase) =
        // stop running children
        let runningChildren = seg.Children.Where(fun ch -> ch.Status = Nullable Status4.Going)
        if seg.Children.Any() then
            noop()
        for child in runningChildren do
            noop()
        stopMonitor goingSubscriptions seg

    let stopMonitorHoming (seg:SegmentBase) =
        // stop homing children
        let homingChildren = seg.Children.Where(fun ch -> ch.Status = Nullable Status4.Homing)
        if seg.Children.Any() then
            noop()
        for child in homingChildren do
            noop()
        stopMonitor homingSubscriptions seg

    let stopMonitorOriginating (seg:SegmentBase) =
        stopMonitor originatingSubscriptions seg


    /// tag 값의 변경으로 인해, going 중 finish 상태가 되는 child 가 존재하면 그것의 start 출력을 끊는다.
    let turnOffStartForFinishedChildren(write:BitWriter, tag:Tag, monitoringChildren:Child seq) =
        task {
            let finishedChildren =
                monitoringChildren
                    .Where(fun ch ->
                        ch.Status = Nullable Status4.Going
                        && ch.TagsEnd.Contains(tag)
                        && ch.TagsEnd.ForAll(fun t -> t.Value))
                        .ToArray()
            assert(finishedChildren.Length = 0 || finishedChildren.Length = 1 )
            let finishedChild = finishedChildren.FirstOrDefault()
            if finishedChild <> null then
                logInfo $"Progress: Child {finishedChild.QualifiedName} originated."
                for st in finishedChild.TagsStart do
                    do! write(st, false, $"Child {finishedChild.QualifiedName} originated")
            return finishedChild
        }


    let procReady(segment:SegmentBase, writer:ChangeWriter) : WriteResult =
        let seg = segment :?> Segment
        assert( [seg.TagPStart; seg.TagPReset; seg.TagPEnd].ForAll(fun t -> not t.Value ))     // A_F_Pp.TagEnd 가 true 되는 상태???
        assert( [seg.PortS :> PortInfo; seg.PortR; seg.PortE].ForAll(fun t -> not t.Value ))
        stopMonitorHoming seg
        task {
            do! writer(BitChange(seg.Ready, true, $"processing ready for {seg.QualifiedName}"))
            do! writer(BitChange(seg.TagPReset, false, $"processing ready for {seg.QualifiedName}"))
        }

    /// Going tag ON 발송 후,
    ///     - child 가 하나라도 있으면, child 의 종료를 모니터링하기 위한 subscription 후, 최초 child group(init) 만 수행
    ///         * 이후, child 종료를 감지하면, 다음 실행할 child 계속 실행하고, 없으면 해당 segment 종료
    ///     - 없으면 바로 종료
    let procGoing(seg:SegmentBase, writer:ChangeWriter) : WriteResult =
        assert( not <| homingSubscriptions.ContainsKey(seg))
        let write:BitWriter = getBitWriter writer

        if isChildrenOrigin(seg) then
            task {
                do! write(seg.Going, true, $"{seg.QualifiedName} Segment GOING 시작")
                assert(seg.Going.Value)
                if seg.Children.Any() then
                    (not <| goingSubscriptions.ContainsKey(seg)) |> verifyM $"Going subscription for {seg.QualifiedName} not empty"
                    let childRxTags = seg.Children.selectMany(fun ch -> ch.TagsEnd).ToArray()
                    
                    let subs =
                        Global.TagChangedSubject
                            .Where(fun t -> t.Value)
                            .Where(childRxTags.Contains)
                            .Subscribe(fun tag ->
                                task {
                                    try
                                        let! finishedChild = turnOffStartForFinishedChildren(write, tag, seg.Children)
                                        if finishedChild <> null then
                                            logInfo $"Progress: Child {finishedChild.QualifiedName} finish detected."
                                            finishedChild.Status <- Status4.Finished
                                            finishedChild.IsFlipped <- true
                            
                                            if (seg.Children.ForAll(fun ch -> ch.IsFlipped)) then
                                                do! write(seg.PortE, true, $"{seg.QualifiedName} GOING 끝 (모든 child end)")
                                            else
                                                // 남은 children 중에서 다음 뒤집을 target 선정후 뒤집기
                                                let targets =
                                                    let edges =
                                                        let finishedChildren = seg.Children.Where(fun ch -> ch.IsFlipped).ToArray()
                                                        seg.Edges
                                                            .Where(fun e -> box e :? ISetEdge)
                                                            .Where(fun e ->
                                                                e.Sources.OfType<Child>()
                                                                    .ForAll(finishedChildren.Contains))
                                                    edges.Select(fun e -> e.Target)
                                                        .OfType<Child>()
                                                        .Where(fun ch -> ch.Status <> Nullable Status4.Going && not ch.IsFlipped)
                                                        .Distinct()
                                                        .ToArray()

                                                do! runChildren (targets, writer)
                                    with exn ->
                                        failwithlog $"{exn}"
                                } |> Async.AwaitTask |> Async.Start
                            )
                    goingSubscriptions.TryAdd(seg, subs) |> verifyM "Failed to add Going subscription"

                    let seg = seg :?> Segment
                    do! runChildren (seg.Inits, writer)

                else // no children
                    do! write(seg.PortE, true, $"{seg.QualifiedName} GOING 끝")
            }// |> Async.AwaitTask |> Async.RunSynchronously

        else // children not at origin
            let outofOriginChildren = getOutofOriginChildren seg
            let subs = 
                Global.PortChangedSubject
                    .Where(fun pc -> pc.Bit = seg.PortE && not pc.NewValue)
                    .Subscribe(fun pc ->
                        stopMonitorOriginating seg
                        for ch in outofOriginChildren do
                            ch.Status <- Status4.Ready
                            ch.DbgIsOriginating <- false
                        doGoing(seg, writer)
                        |> ignore
                        )

            seg.DbgIsOriginating <- true
            for ch in outofOriginChildren do
                ch.DbgIsOriginating <- true
            let t = doHoming(seg, writer)
            originatingSubscriptions.TryAdd(seg, subs) |> verifyM "Failed to add Originating subscription"
            t

    let procFinish(segment:SegmentBase, writer:ChangeWriter) : WriteResult =
        let seg = segment :?> Segment
        let write = getBitWriter writer

        if seg.QualifiedName = "L_F_Main" then
            noop()

        stopMonitorGoing seg
        task {
            assert (not seg.Going.Value)
            assert seg.TagPEnd.Value
            //do! write(seg.Going, false, $"{seg.QualifiedName} FINISH")
            //do! write(seg.TagPEnd, true, $"Finishing {seg.QualifiedName}")
            do! write(seg.TagPStart, false, $"Finishing {seg.QualifiedName}")
        }

    let procHoming(segment:SegmentBase, writer:ChangeWriter) : WriteResult =
        let seg = segment :?> Segment

        if seg.QualifiedName = "L_F_Main" then
            noop()

        let write:BitWriter = getBitWriter writer

        stopMonitorGoing seg

        let originTargets = getChildrenOrigin seg  // todo: 원위치 맞출 children
        for ch in seg.Children do
            ch.IsFlipped <- false

        task {
            if isChildrenOrigin seg then
                do! write(seg.PortE, false, $"{seg.QualifiedName} HOMING finished")
            else
                // 자식 원위치 맞추기
                let childRxTags = originTargets.selectMany(fun ch -> ch.TagsEnd).ToArray()
                assert(originTargets.Any())
                let subs =
                    Global.TagChangedSubject
                        .Where(fun t -> t.Value)    // ON child
                        .Where(childRxTags.Contains)
                        .Subscribe(fun tag ->
                            task {
                                let! finishedChild = turnOffStartForFinishedChildren(write, tag, originTargets)
                                // originTargets 모두 원위치인지 확인
                                let isOrigin = originTargets.ForAll(fun ch -> ch.TagsEnd.ForAll(fun t -> t.Value))
                                if isOrigin then
                                    stopMonitorHoming seg
                                    do! write(seg.PortE, false, $"{seg.QualifiedName} HOMING finished")
                            } |> Async.AwaitTask |> Async.Start
                        )
                homingSubscriptions.TryAdd(seg, subs) |> verifyM "Failed to add Homing subscription"

                do! runChildren(originTargets, writer)

                //// todo: fix me
                //let et = seg.TagPEnd
                ////if et.Type.HasFlag(TagType.External) then
                ////    ()
                ////else
                //if et.Value then
                //    //assert(false)
                //    write(et, false, $"내부 end tag {et.Name} 강제 off by {segment.QualifiedName} homing")

        }