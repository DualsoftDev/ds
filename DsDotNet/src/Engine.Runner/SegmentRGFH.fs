namespace Engine.Runner

open System
open System.Linq
open System.Reactive.Linq
open System.Collections.Generic

open Engine.Common.FS
open Engine.Core
open Engine.Common


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

    let runChildren (children:Child seq, writer:ChangeWriter, onError:ExceptionHandler) =
        assert(children.Distinct().Count() = children.Count())
        for child in children do
            child.Status <- Status4.Going

        for child in children do
            assert(not child.IsFlipped && (not child.Status.HasValue || child.Status.Value = Status4.Going))
            logInfo $"Child {child.QualifiedName} starting.."
            assert(child.TagsStart.Count <= 1)  // 일단... 체크용..
            for st in child.TagsStart do
                let before = fun () ->
                    //child.Status <- Status4.Going
                    Global.ChildStatusChangedSubject.OnNext(ChildStatusChange(child, Status4.Going))

                writer(BitChange(st, true, "Starting child", onError, BeforeAction = before))

    /// Segment 별로 Going 중에 child 의 종료 모니터링.  segment 가 Going 이 아니게 되면, dispose
    let goingSubscriptions = Dictionary<SegmentBase, IDisposable>()
    let homingSubscriptions = Dictionary<SegmentBase, IDisposable>()
    /// 원위치가 맞지 않은 상태에서 Going 시, 원위치부터 맞추기 위한 작업
    let originatingSubscriptions = Dictionary<SegmentBase, IDisposable>()

    let stopMonitor (subscriptions:Dictionary<SegmentBase, IDisposable>) (seg:SegmentBase) =
        if subscriptions.ContainsKey(seg) then
            let subs = subscriptions[seg]
            subscriptions.Remove(seg) |> ignore
            subs.Dispose()
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

    let procReady(seg:SegmentBase, writer:ChangeWriter, onError:ExceptionHandler) =
        let write:BitWriter = getBitWriter writer onError
        assert( [seg.TagPStart; seg.TagPReset; seg.TagPEnd].ForAll(fun t -> not t.Value ))     // A_F_Pp.TagEnd 가 true 되는 상태???
        assert( [seg.PortS :> PortInfo; seg.PortR; seg.PortE].ForAll(fun t -> not t.Value ))
        stopMonitorHoming seg
        write(seg.Ready, true, null)

    /// Going tag ON 발송 후,
    ///     - child 가 하나라도 있으면, child 의 종료를 모니터링하기 위한 subscription 후, 최초 child group(init) 만 수행
    ///         * 이후, child 종료를 감지하면, 다음 실행할 child 계속 실행하고, 없으면 해당 segment 종료
    ///     - 없으면 바로 종료
    let procGoing(seg:SegmentBase, writer:ChangeWriter, onError:ExceptionHandler) =
        assert( not <| homingSubscriptions.ContainsKey(seg))
        let write:BitWriter = getBitWriter writer onError
        let writeEndPort = getEndPortPlanWriter writer onError

        if isChildrenOrigin(seg) then
            write(seg.Going, true, $"{seg.QualifiedName} GOING 시작")
            if seg.Children.Any() then
                assert(not <| goingSubscriptions.ContainsKey(seg))
                let childRxTags = seg.Children.selectMany(fun ch -> ch.TagsEnd).ToArray()
                    
                let subs =
                    Global.TagChangedSubject
                        .Where(fun t -> t.Value)
                        .Where(childRxTags.Contains)
                        .Subscribe(fun tag ->
                            let finishedChildren =
                                seg.Children
                                    .Where(fun ch ->
                                        ch.Status = Nullable Status4.Going
                                        && ch.TagsEnd.Contains(tag)
                                        && ch.TagsEnd.ForAll(fun t -> t.Value))
                                        .ToArray()
                            assert(finishedChildren.Length = 0 || finishedChildren.Length = 1 )
                            let finishedChild = finishedChildren.FirstOrDefault()
                            if finishedChild <> null then
                                logInfo $"Child {finishedChild.QualifiedName} finish detected."
                                finishedChild.Status <- Status4.Finished
                                finishedChild.IsFlipped <- true
                                for st in finishedChild.TagsStart do
                                    assert(not st.Value)
                                    //write(st, false, $"Child {finishedChild.QualifiedName} finished")
                            
                                if (seg.Children.ForAll(fun ch -> ch.IsFlipped)) then
                                    writeEndPort(seg.PortE, true, $"{seg.QualifiedName} GOING 끝 (모든 child end)")
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

                                    runChildren (targets, writer, onError)
                        )
                goingSubscriptions.Add(seg, subs)

                let seg = seg :?> Segment
                runChildren (seg.Inits, writer, onError)

            else
                writeEndPort(seg.PortE, true, $"{seg.QualifiedName} GOING 끝")
        else    // children not at origin
            let outofOriginChildren = getOutofOriginChildren seg
            let subs = 
                Global.PortChangedSubject
                    .Where(fun pc -> pc.Bit = seg.PortE && not pc.NewValue)
                    .Subscribe(fun pc ->
                        stopMonitorOriginating seg
                        for ch in outofOriginChildren do
                            ch.Status <- Status4.Ready
                        doGoing(seg, writer, onError)
                        )
            doHoming(seg, writer, onError)
            originatingSubscriptions.Add(seg, subs)

    let procFinish(seg:SegmentBase, writer:ChangeWriter, onError:ExceptionHandler) =
        let write = getBitWriter writer onError

        stopMonitorGoing seg
        write(seg.Going, false, $"{seg.QualifiedName} FINISH")
        if seg.TagPEnd.Value then
            noop()
        write(seg.TagPEnd, true, $"Finishing {seg.QualifiedName}")

    let procHoming(segment:SegmentBase, writer:ChangeWriter, onError:ExceptionHandler) =
        let seg = segment :?> Segment
        let write:BitWriter = getBitWriter writer onError
        let writeEndPort = getEndPortPlanWriter writer onError

        stopMonitorGoing seg
        // 자식 원위치 맞추기
        let originTargets = getChildrenOrigin seg  // todo: 원위치 맞출 children
        let childRxTags = originTargets.selectMany(fun ch -> ch.TagsEnd).ToArray()

        if originTargets.Any() then                    
            let subs =
                Global.TagChangedSubject
                    .Where(fun t -> t.Value)    // ON child
                    .Where(childRxTags.Contains)
                    .Subscribe(fun tag ->
                        // originTargets 모두 원위치인지 확인
                        let isOrigin = originTargets.ForAll(fun ch -> ch.TagsEnd.ForAll(fun t -> t.Value))
                        if isOrigin then
                            stopMonitorHoming seg
                            writeEndPort(seg.PortE, false, $"{seg.QualifiedName} HOMING finished")
                    )
            homingSubscriptions.Add(seg, subs)

            runChildren(originTargets, writer, onError)

        else
            writeEndPort(seg.PortE, false, $"{seg.QualifiedName} HOMING finished")

            // todo: fix me
            let et = seg.TagPEnd
            //if et.Type.HasFlag(TagType.External) then
            //    ()
            //else
            if et.Value then
                //assert(false)
                write(et, false, $"내부 end tag {et.Name} 강제 off by {segment.QualifiedName} homing")
