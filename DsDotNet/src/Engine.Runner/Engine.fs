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
            let roots = rootFlows.selectMany(fun rf -> rf.RootSegments).Cast<FsSegment>()

            //let _makeUpSegmentBits =
            //    for root in roots do
            //        let cpu = root.ContainerFlow.Cpu
            //        let n = root.QualifiedName
            //        if isNull root.Going then
            //            root.Going <- Tag(cpu, root, $"Going_TEMP_{n}", TagType.Going)
            //        if isNull root.Ready then
            //            root.Ready <- Tag(cpu, root, $"Ready_TEMP_{n}", TagType.Ready)


            //        //if not <| root.TagsEnd.Any(fun t -> t.Type.HasFlag(TagType.Flow)) then
            //        //    root.AddEndTags([|Tag(cpu, root, $"FlowEnd_{n}", TagType.End|||TagType.Flow)|])
            //        if isNull root.PortE then
            //            root.PortE <- PortInfoEnd.Create(cpu, root, $"epex{n}_default", null)

            //        if isNull root.PortS then
            //            root.PortS <- PortInfoStart(cpu, root, $"spex{n}_default", root.TagStart, null)
            //        if isNull root.PortR then
            //            root.PortR <- PortInfoReset(cpu, root, $"rpex{n}_default", root.TagReset, null)

                
            // todo : 가상 부모 생성
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
