namespace Engine.Runner

open System
open System.Linq
open System.Reactive.Linq

open Engine.Common.FS
open Engine.Core
open Engine.Common
open System.Collections.Concurrent
open System.Threading.Tasks
open Engine.Base


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
    let runChildren (children:Child seq, writer:BitWriter) =
        assert(children.Distinct().Count() = children.Count())

        [   for child in children do
                //assert(not child.IsFlipped && (not child.Status.HasValue || child.Status.IsOneOf(Status4.Ready, Status4.Finish)))
                logInfo $"Progress: Child {child.QualifiedName} starting.."

                for st in child.TagsStart do
                    child.Status <- Status4.Going
                    Global.ChildStatusChangedSubject.OnNext(ChildStatusChange(child, Status4.Going))
                    yield writer(st, true, "Starting child")
        ] |> Async.Parallel |> Async.Ignore

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
        let runningChildren = seg.Children.Where(fun ch -> ch.Status = Status4.Going)
        stopMonitor goingSubscriptions seg

    let stopMonitorHoming (seg:SegmentBase) =
        // stop homing children
        let homingChildren = seg.Children.Where(fun ch -> ch.Status = Status4.Homing)
        stopMonitor homingSubscriptions seg

    let stopMonitorOriginating (seg:SegmentBase) =
        stopMonitor originatingSubscriptions seg


    /// tag 값의 변경으로 인해, going 중 finish 상태가 되는 child 가 존재하면 그것의 start 출력을 끊는다.
    let turnOffStartForFinishedChildren(write:BitWriter, tag:Tag, monitoringChildren:Child seq) : Async<Child> =
        async {
            let finishedChildren =
                monitoringChildren
                    .Where(fun ch ->
                        ch.Status =  Status4.Going
                        && ch.TagsEnd.Contains(tag)
                        && ch.TagsEnd.ForAll(fun t -> t.Value))
                        .ToArray()
            assert(finishedChildren.Length = 0 || finishedChildren.Length = 1 )
            let finishedChild = finishedChildren.FirstOrDefault()
            if finishedChild <> null then
                logInfo $"Progress: Child {finishedChild.QualifiedName} originated."
                for st in finishedChild.TagsStart do
                    write(st, false, $"Child {finishedChild.QualifiedName} originated") |> Async.Start
            return finishedChild
        }


    let procReady(segment:SegmentBase) : WriteResult =
        let seg = segment :?> Segment
        let write = seg.AsyncWrite
        assert( [seg.TagPStart; seg.TagPReset; seg.TagPEnd].ForAll(fun t -> not t.Value ))     // A_F_Pp.TagEnd 가 true 되는 상태???
        assert( [seg.PortS :> PortInfo; seg.PortR; seg.PortE].ForAll(fun t -> not t.Value ))
        stopMonitorHoming seg   // normal case
        stopMonitorGoing seg    // going 중에 start 끊긴 경우의 대비
        write(seg.Ready, true, $"processing ready for {seg.QualifiedName}")
        //assert (seg.TagPReset.Value = false)

    /// Going tag ON 발송 후,
    ///     - child 가 하나라도 있으면, child 의 종료를 모니터링하기 위한 subscription 후, 최초 child group(init) 만 수행
    ///         * 이후, child 종료를 감지하면, 다음 실행할 child 계속 실행하고, 없으면 해당 segment 종료
    ///     - 없으면 바로 종료
    let procGoing(seg:SegmentBase) : WriteResult =
        let seg = seg :?> Segment
        assert( not <| homingSubscriptions.ContainsKey(seg))
        let write = seg.AsyncWrite

        /// Children 의 going 상태를 지켜 보면서 다음 child 를 순차적으로 실행
        let createChildrenGoingMonitor() =
            let childRxTags = seg.Children.selectMany(fun ch -> ch.TagsEnd).ToArray()
            Global.TagChangedSubject
                .Where(fun t -> t.Value)
                .Where(childRxTags.Contains)
                .Subscribe(fun tag ->
                    async {
                        try
                            let! finishedChild = turnOffStartForFinishedChildren(write, tag, seg.Children)
                            if finishedChild <> null then
                                logInfo $"Progress: Child {finishedChild.QualifiedName} finish detected."
                                finishedChild.Status <- Status4.Finish
                                finishedChild.IsFlipped <- true
                            
                                if (seg.Children.ForAll(fun ch -> ch.IsFlipped)) then
                                    write(seg.PortE, true, $"{seg.QualifiedName} GOING 끝 (모든 child end)") |> Async.Start
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
                                            .Where(fun ch -> ch.Status <>  Status4.Going && not ch.IsFlipped)
                                            .Distinct()
                                            .ToArray()

                                    runChildren (targets, write) |> Async.Start
                        with exn ->
                            failwithlog $"{exn}"
                    } |> Async.Start
                )

        if isChildrenOrigin(seg) then
            write(seg.Going, true, $"{seg.QualifiedName} Segment GOING 시작") |> Async.Start
            //assert(seg.Going.Value)
            if seg.Children.Any() then
                (not <| goingSubscriptions.ContainsKey(seg)) |> verifyM $"Going subscription for {seg.QualifiedName} not empty"                    
                let subs = createChildrenGoingMonitor()
                goingSubscriptions.TryAdd(seg, subs) |> verifyM "Failed to add Going subscription"

                runChildren (seg.Inits, write)

            else // no children
                write(seg.PortE, true, $"{seg.QualifiedName} GOING 끝")

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
                        doGoing seg |> Async.Start )

            seg.DbgIsOriginating <- true
            for ch in outofOriginChildren do
                ch.DbgIsOriginating <- true
            originatingSubscriptions.TryAdd(seg, subs) |> verifyM "Failed to add Originating subscription"
            doHoming seg

    let procFinish(segment:SegmentBase) : WriteResult =
        let seg = segment :?> Segment
        let write = seg.AsyncWrite

        stopMonitorGoing seg

        write(seg.TagPStart, false, $"Finishing {seg.QualifiedName}")

    let procHoming(segment:SegmentBase) : WriteResult =
        let seg = segment :?> Segment
        let write = seg.AsyncWrite

        stopMonitorGoing seg

        let originTargets = getChildrenOrigin seg  // todo: 원위치 맞출 children
        for ch in seg.Children do
            ch.IsFlipped <- false

        if isChildrenOrigin seg then
            write(seg.PortE, false, $"{seg.QualifiedName} HOMING finished")
        else
            // 자식 원위치 맞추기
            let childRxTags = originTargets.selectMany(fun ch -> ch.TagsEnd).ToArray()
            assert(originTargets.Any())
            let subs =
                Global.TagChangedSubject
                    .Where(fun t -> t.Value)    // ON child
                    .Where(childRxTags.Contains)
                    .Subscribe(fun tag ->
                        async {
                            let! finishedChild = turnOffStartForFinishedChildren(write, tag, originTargets)
                            // originTargets 모두 원위치인지 확인
                            let isOrigin = originTargets.ForAll(fun ch -> ch.TagsEnd.ForAll(fun t -> t.Value))
                            if isOrigin then
                                stopMonitorHoming seg
                                do! write(seg.PortE, false, $"{seg.QualifiedName} HOMING finished")
                        } |> Async.Start
                    )
            homingSubscriptions.TryAdd(seg, subs) |> verifyM "Failed to add Homing subscription"

            runChildren(originTargets, write)