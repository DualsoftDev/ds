namespace Engine.Runner

open System
open System.Linq
open System.Reactive.Disposables

open Dual.Common
open Engine.Core
open Engine.OPC
open System.Threading
open System.Reactive.Linq


[<AutoOpen>]
module EngineModule =
    let Initialize() =
        SegmentBase.Create <-
            new Func<string, RootFlow, SegmentBase>(
                fun name (rootFlow:RootFlow) ->
                    let seg = Segment(rootFlow.Cpu, name)
                    seg.ContainerFlow <- rootFlow
                    rootFlow.AddChildVertex(seg)
                    seg)

    /// 외부에서 tag 가 변경된 경우 수행할 작업 지정
    let private onOpcTagChanged (cpu:Cpu) (tagChange:OpcTagChange) =
        let tagName, value = tagChange.TagName, tagChange.Value
        if (cpu.TagsMap.ContainsKey(tagName)) then
            let tag = cpu.TagsMap[tagName]
            if tag.Value <> value then
                cpu.Enqueue(tag, value, $"OPC Tag [{tagName}] 변경");      //! setter 에서 BitChangedSubject.OnNext --> onBitChanged 가 호출된다.

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
                [
                    for rf in rootFlows do
                        yield! VirtualParentSegmentModule.CreateVirtualParentSegmentsFromRootFlow(rf)
                ]


            let unparentedRoots =
                let parentedRoots = virtualParentSegments.Select(fun vps -> vps.Target)
                roots.Except(parentedRoots) 

            virtualParentSegments
            |> Seq.iter(fun vps ->
                vps.Target.WireEvent(vps.Cpu.Enqueue, raise) |> ignore
                vps.WireEvent(vps.Cpu.Enqueue, raise) |> ignore
                )
            unparentedRoots
            |> Seq.iter(fun seg ->
                seg.WireEvent(seg.Cpu.Enqueue, raise) |> ignore
                )

            assert( virtualParentSegments |> Seq.forall(fun vp -> vp.Status = Status4.Ready));
            assert( unparentedRoots       |> Seq.forall(fun vp -> vp.Status = Status4.Ready));


            logInfo "Start F# Engine running..."
            for cpu in cpus do
                cpu.BuildBitDependencies()
                //cpu.PrintTags()

                logDebug "====================="
                cpu.PrintAllTags(false);
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

                        // todo : 코멘트 처리 해제?
                        //yield runCpu cpu  // ! 실제 수행!!
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
