namespace Engine.Runner

open Engine.Core
open Engine.Common
open System
open System.Linq
open System.Reactive.Linq
open Dual.Common
open System.Collections.Generic


[<AutoOpen>]
module RGFHModule =

    type Writer = IBit*bool*obj -> unit

    /// Segment 별로 Going 중에 child 의 종료 모니터링.  segment 가 Going 이 아니게 되면, dispose
    let private goingSubscriptions = Dictionary<Segment, IDisposable>()
    let private homingSubscriptions = Dictionary<Segment, IDisposable>()
    let private stopMonitor (subscriptions:Dictionary<Segment, IDisposable>) (seg:Segment) =
        if subscriptions.ContainsKey(seg) then
            let subs = subscriptions[seg]
            subscriptions.Remove(seg) |> ignore
            subs.Dispose()
    let private stopMonitorGoing seg = stopMonitor goingSubscriptions seg
    let private stopMonitorHoming seg = stopMonitor homingSubscriptions seg


    let doReady(write:Writer, seg:Segment) =
        stopMonitorHoming seg
        write(seg.Ready, true, null)

    /// Going tag ON 발송 후,
    ///     - child 가 하나라도 있으면, child 의 종료를 모니터링하기 위한 subscription 후, 최초 child group(init) 만 수행
    ///         * 이후, child 종료를 감지하면, 다음 실행할 child 계속 실행하고, 없으면 해당 segment 종료
    ///     - 없으면 바로 종료
    let doGoing(write:Writer, seg:Segment, tagFlowReset:Tag) =
        assert( not <| homingSubscriptions.ContainsKey(seg))
        write(seg.Going, true, $"{seg.QualifiedName} GOING 시작")
        if seg.Children.Any() then
            let runChildren(children:Child seq) =
                for child in children do
                    let st = child.TagsStart.First(fun t -> t.Type.HasFlag(TagType.Flow) || t.Type.HasFlag(TagType.TX))
                    assert(not child.IsFlipped && (not child.Status.HasValue || child.Status.Value = Status4.Going))
                    child.Status <- Status4.Going
                    write(st, true, "Starting child")

            assert(not <| goingSubscriptions.ContainsKey(seg))
            let startTasg = seg.Inits.SelectMany(fun ch -> ch.TagsStart)
            let childRxTags = seg.Children.SelectMany(fun ch -> ch.TagsEnd).ToArray()
                    
            let subs =
                Global.TagChangedSubject
                    .Where(fun t -> t.Value)
                    .Where(childRxTags.Contains)
                    .Subscribe(fun tag ->
                        let finishedChild = seg.Children.First(fun ch -> ch.TagsEnd.Contains(tag))
                        logDebug $"Child {finishedChild.QualifiedName} finish detected."
                        finishedChild.Status <- Status4.Finished
                        finishedChild.IsFlipped <- true
                        if (seg.Children.ForAll(fun ch -> ch.IsFlipped)) then
                            write(seg.PortE, true, $"{seg.QualifiedName} GOING 끝 (모든 child end)")
                        else
                            // 남은 children 중에서 다음 뒤집을 target 선정후 뒤집기
                            let targets =
                                let edges =
                                    let finishedChildren = seg.Children.Where(fun ch -> ch.IsFlipped).ToArray()
                                    seg.Edges
                                        .Where(fun e ->
                                            e.Sources.OfType<Child>()
                                                .ForAll(finishedChildren.Contains))
                                edges.Select(fun e -> e.Target).OfType<Child>().Where(fun e -> not e.IsFlipped)

                            runChildren targets
                    )
            goingSubscriptions.Add(seg, subs)

            runChildren seg.Inits

        else
            write(seg.PortE, true, $"{seg.QualifiedName} GOING 끝")


    let doFinish(write:Writer, seg:Segment) =
        stopMonitorGoing seg
        write(seg.Going, false, $"{seg.QualifiedName} FINISH")
        for et in seg.TagsEnd do
            write(et, true, $"Finishing {seg.QualifiedName}")

    let doHoming(write:Writer, seg:Segment) =
        stopMonitorGoing seg
        // 자식 원위치 맞추기
        let childRxTags = seg.Children.SelectMany(fun ch -> ch.TagsEnd).ToArray()
        let originTargets = Seq.empty  // todo: 원위치 맞출 children

        if originTargets.Any() then                    
            let subs =
                Global.TagChangedSubject
                    .Where(fun t -> not t.Value)    // OFF child
                    .Where(childRxTags.Contains)
                    .Subscribe(fun tag ->
                        // originTargets 모두 원위치인지 확인
                        write(seg.PortE, false, $"{seg.QualifiedName} HOMING finished")
                        ()
                    )
            homingSubscriptions.Add(seg, subs)
        else
            write(seg.PortE, false, $"{seg.QualifiedName} HOMING finished")

