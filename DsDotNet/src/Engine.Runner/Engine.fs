namespace Engine.Runner

open System
open System.Linq
open System.Reactive.Disposables
open System.Threading
open System.Reactive.Linq
open System.Collections.Generic

open Engine.Common.FS
open Engine.Core
open Engine.OPC
open Engine.Common


[<AutoOpen>]
module EngineModule =
    /// 외부에서 tag 가 변경된 경우 수행할 작업 지정
    let private onOpcTagChanged (cpu:Cpu) (tagChange:OpcTagChange) =
        let tagName, value = tagChange.TagName, tagChange.Value
        if (cpu.TagsMap.ContainsKey(tagName)) then
            let tag = cpu.TagsMap[tagName]
            if tag.Value <> value then
                cpu.Enqueue(tag, value, $"OPC Tag [{tagName}] 변경");      //! setter 에서 BitChangedSubject.OnNext --> onBitChanged 가 호출된다.



    /// Segment 별로 Going 중에 child 의 종료 모니터링.  segment 가 Going 이 아니게 되면, dispose
    let private goingSubscriptions = Dictionary<SegmentBase, IDisposable>()
    let private homingSubscriptions = Dictionary<SegmentBase, IDisposable>()
    let private stopMonitor (subscriptions:Dictionary<SegmentBase, IDisposable>) (seg:SegmentBase) =
        if subscriptions.ContainsKey(seg) then
            let subs = subscriptions[seg]
            subscriptions.Remove(seg) |> ignore
            subs.Dispose()
    let private stopMonitorGoing (seg:SegmentBase) =
        // stop running children
        let runningChildren = seg.Children.Where(fun ch -> ch.Status = Nullable Status4.Going)
        if seg.Children.Any() then
            noop()
        for child in runningChildren do
            ()

        stopMonitor goingSubscriptions seg

    let private stopMonitorHoming (seg:SegmentBase) =
        // stop homing children
        let homingChildren = seg.Children.Where(fun ch -> ch.Status = Nullable Status4.Homing)
        if seg.Children.Any() then
            noop()
        for child in homingChildren do
            ()
        stopMonitor homingSubscriptions seg

    let private procReady(seg:SegmentBase, writer:ChangeWriter, onError:ExceptionHandler) =
        let write(bit, value, cause) =
            writer(BitChange(bit, value, cause, onError))
        assert( [seg.TagStart; seg.TagReset; seg.TagEnd].ForAll(fun t -> not t.Value ))     // A_F_Pp.TagEnd 가 true 되는 상태???
        assert( [seg.PortS :> PortInfo; seg.PortR; seg.PortE].ForAll(fun t -> not t.Value ))
        stopMonitorHoming seg
        write(seg.Ready, true, null)

    /// Going tag ON 발송 후,
    ///     - child 가 하나라도 있으면, child 의 종료를 모니터링하기 위한 subscription 후, 최초 child group(init) 만 수행
    ///         * 이후, child 종료를 감지하면, 다음 실행할 child 계속 실행하고, 없으면 해당 segment 종료
    ///     - 없으면 바로 종료
    let private procGoing(seg:SegmentBase, writer:ChangeWriter, onError:ExceptionHandler) =
        assert( not <| homingSubscriptions.ContainsKey(seg))
        let write(bit, value, cause) =
            writer(BitChange(bit, value, cause, onError))

        write(seg.Going, true, $"{seg.QualifiedName} GOING 시작")
        if seg.Children.Any() then
            let runChildren(children:Child seq) =
                for child in children do
                    assert(not child.IsFlipped && (not child.Status.HasValue || child.Status.Value = Status4.Going))
                    for st in child.TagsStart do
                        let before = fun () ->
                            child.Status <- Status4.Going
                            Global.ChildStatusChangedSubject.OnNext(ChildStatusChange(child, Status4.Going))
                            
                        writer(BitChange(st, true, "Starting child", onError, BeforeAction = before))

            assert(not <| goingSubscriptions.ContainsKey(seg))
            let childRxTags = seg.Children.selectMany(fun ch -> ch.TagsEnd).ToArray()
                    
            let subs =
                Global.TagChangedSubject
                    .Where(fun t -> t.Value)
                    .Where(childRxTags.Contains)
                    .Subscribe(fun tag ->
                        let finishedChild = seg.Children.FirstOrDefault(fun ch -> ch.TagsEnd.Contains(tag) && ch.TagsEnd.ForAll(fun t -> t.Value))
                        if finishedChild <> null then
                            logDebug $"Child {finishedChild.QualifiedName} finish detected."
                            finishedChild.Status <- Status4.Finished
                            finishedChild.IsFlipped <- true
                            for st in finishedChild.TagsStart do
                                write(st, false, $"Child {finishedChild.QualifiedName} finished")
                            
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

            let seg = seg :?> Segment
            runChildren seg.Inits

        else
            write(seg.PortE, true, $"{seg.QualifiedName} GOING 끝")


    let private procFinish(seg:SegmentBase, writer:ChangeWriter, onError:ExceptionHandler) =
        let write(bit, value, cause) =
            writer(BitChange(bit, value, cause, onError))

        stopMonitorGoing seg
        write(seg.Going, false, $"{seg.QualifiedName} FINISH")
        write(seg.TagEnd, true, $"Finishing {seg.QualifiedName}")

    let private procHoming(segment:SegmentBase, writer:ChangeWriter, onError:ExceptionHandler) =
        let seg = segment :?> Segment
        let write(bit, value, cause) =
            writer(BitChange(bit, value, cause, onError))

        stopMonitorGoing seg
        // 자식 원위치 맞추기
        let childRxTags = seg.Children.selectMany(fun ch -> ch.TagsEnd).ToArray()
        let originTargets = seg.ChildrenOrigin  // todo: 원위치 맞출 children

        if originTargets.Any() then                    
            let subs =
                Global.TagChangedSubject
                    .Where(fun t -> not t.Value)    // OFF child
                    .Where(childRxTags.Contains)
                    .Subscribe(fun tag ->
                        let readyChild = seg.Children.FirstOrDefault(fun ch -> ch.TagsEnd.Contains(tag) && ch.TagsEnd.ForAll(fun t -> not t.Value))
                        if readyChild <> null then
                            ()
                        // originTargets 모두 원위치인지 확인
                        write(seg.PortE, false, $"{seg.QualifiedName} HOMING finished")
                        ()
                    )
            homingSubscriptions.Add(seg, subs)
        else
            write(seg.PortE, false, $"{seg.QualifiedName} HOMING finished")

            // todo: fix me
            let et = seg.TagEnd
            if not <| et.Type.HasFlag(TagType.External) then
                write(et, false, $"내부 end tag {et.Name} 강제 off by {segment.QualifiedName} homing")

    let Initialize() =
        SegmentBase.Create <-
            fun name (rootFlow:RootFlow) ->
                let seg = Segment(rootFlow.Cpu, name)
                seg.ContainerFlow <- rootFlow
                rootFlow.AddChildVertex(seg)
                seg

        doReady  <- procReady 
        doGoing  <- procGoing 
        doFinish <- procFinish
        doHoming <- procHoming



    type Engine(model:Model, opc:OpcBroker, activeCpu:Cpu) =
        let cpus = model.Cpus

        interface IEngine
        member _.Model = model
        member _.Opc = opc
        member _.Cpu = activeCpu


        member _.Run() =
            /// OPC Server 에서 Cpu 가 가지고 있는 tag 값들을 읽어 들임
            /// Engine 최초 구동 시, 수행됨.
            let readTagsFromOpc (cpu:Cpu) (opc:OpcBroker) =
                let tpls = opc.ReadTags(cpu.TagsMap.map(fun t -> t.Key))
                for tName, value in tpls do
                    let tag = cpu.TagsMap[tName]
                    if tag.Value <> value then
                        onOpcTagChanged cpu (new OpcTagChange(tName, value))


            let rootFlows = cpus.selectMany(fun cpu -> cpu.RootFlows)
            let roots = rootFlows.selectMany(fun rf -> rf.RootSegments).Cast<Segment>()
                
            // 가상 부모 생성
            let virtualParentSegments =
                rootFlows.selectMany(CreateVirtualParentSegmentsFromRootFlow).ToArray()


            virtualParentSegments
            |> Seq.iter(fun vps ->
                vps.Target.WireEvent(vps.Cpu.Enqueue, raise) |> ignore
                vps.WireEvent(vps.Cpu.Enqueue, raise) |> ignore
                )

            assert( virtualParentSegments |> Seq.forall(fun vp -> vp.Status = Status4.Ready));


            logInfo "Start F# Engine running..."
            for cpu in cpus do
                cpu.BuildBitDependencies()

                //logDebug "====================="
                //cpu.PrintAllTags(false);
                logDebug "---------------------"
                cpu.PrintAllTags(true);
                logDebug "====================="


            let subscriptions =
                [
                    // OPC server 쪽에서 tag 값 변경시, 해당 tag 를 가지고 있는 모든 CPU 에 event 를 전달한다.
                    yield Global.TagChangeFromOpcServerSubject
                        .Subscribe(fun tc ->
                            cpus
                            |> Seq.filter(fun cpu -> cpu.TagsMap.ContainsKey(tc.TagName))
                            |> Seq.iter(fun cpu -> onOpcTagChanged cpu tc))

                    for cpu in cpus do
                        readTagsFromOpc cpu opc

                        yield cpu.Run()
                ]

            let _autoStart =
                for cpu in cpus do
                for rf in cpu.RootFlows do
                    cpu.Enqueue(rf.Auto, true, "Auto Flow start")

            new CompositeDisposable(subscriptions)

        member _.Wait() =
            use _subs =
                Observable.Interval(TimeSpan.FromSeconds(10))
                    .Subscribe(fun t ->
                        let runningCpus = String.Join(", ", cpus.Where(fun cpu -> cpu.Running))
                        logDebug $"Running cpus: {runningCpus}")
            while cpus.Any(fun cpu -> cpu.Running) do
                Thread.Sleep(50)

