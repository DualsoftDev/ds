namespace Engine.Runner

open System
open System.Linq
open System.Reactive.Disposables
open System.Threading
open System.Reactive.Linq

open Engine.Common.FS
open Engine.Core
open Engine.OPC

[<AutoOpen>]
module EngineModule =
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
            /// 외부에서 tag 가 변경된 경우 수행할 작업 지정
            let onOpcTagChanged (cpu:Cpu) (tagChange:OpcTagChange) =
                let tagName, value = tagChange.TagName, tagChange.Value
                if (cpu.TagsMap.ContainsKey(tagName)) then
                    let tag = cpu.TagsMap[tagName]
                    if tag.Value <> value then
                        logDebug $"OPC Tag 변경 [{tagName}={value}] : cpu={cpu}"
                        cpu.Enqueue(tag, value, $"OPC Tag 변경 [{tagName}={value}]")      //! setter 에서 BitChangedSubject.OnNext --> onBitChanged 가 호출된다.
                        |> ignore

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
                rootFlows.selectMany(createVPSsFromRootFlow).ToArray()

            Global.Model.VPSs <- virtualParentSegments.Cast<SegmentBase>().ToArray()

            virtualParentSegments
            |> Seq.iter(fun vps ->
                let writer:ChangeWriter = vps.Cpu.Enqueue
                //let writer = vps.Cpu.SendChange
                vps.Target.WireEvent(writer) |> ignore
                vps.WireEvent(writer) |> ignore
                )

            assert( virtualParentSegments |> Seq.forall(fun vp -> vp.Status = Status4.Ready));


            logInfo "Start F# Engine running..."

            // printModel model

            for cpu in cpus do
                cpu.BuildBitDependencies()

                //logDebug "====================="
                //cpu.PrintAllTags(false);
                logDebug "---------------------"
                cpu.PrintAllTags(true);
                logDebug "====================="

            Thread.Sleep(1000)

            let subscriptions =
                [
                    // OPC server 쪽에서 tag 값 변경시, 해당 tag 를 가지고 있는 모든 CPU 에 event 를 전달한다.
                    yield Global.TagChangeFromOpcServerSubject
                        .Subscribe(fun tc ->
                            let cpusA = cpus |> Seq.filter(fun cpu -> cpu.TagsMap.ContainsKey(tc.TagName)) |> Array.ofSeq
                            cpusA |> Seq.iter(fun cpu -> onOpcTagChanged cpu tc))

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

