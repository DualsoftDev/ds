namespace Engine.Runner

open System
open System.Linq
open System.Reactive.Disposables

open Dual.Common
open Engine.Core
open Engine.OPC


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


            let roots = activeCpu.RootFlows.selectMany(fun rf -> rf.RootSegments).Cast<FsSegment>()

            let _makeUpSegmentBits =
                for root in roots do
                    let cpu = root.ContainerFlow.Cpu
                    let n = root.QualifiedName
                    if isNull root.Going then
                        root.Going <- Tag(cpu, root, $"Going_TEMP_{n}", TagType.Going)
                    if not <| root.TagsStart.Any(fun t -> t.Type.HasFlag(TagType.Flow)) then
                        root.AddStartTags([|Tag(cpu, root, $"FlowStart_{n}", TagType.Start|||TagType.Flow)|])
                    if not <| root.TagsReset.Any(fun t -> t.Type.HasFlag(TagType.Flow)) then
                        root.AddResetTags([|Tag(cpu, root, $"FlowReset_{n}", TagType.Reset|||TagType.Flow)|])
                
            // todo : 가상 부모 생성
            let virtualParentSegments =
                [
                    for rf in activeCpu.RootFlows do
                        yield! VirtualParentSegmentModule.CreateVirtualParentSegmentsFromRootFlow(rf)
                ]



            let unparentedRoots =
                let parentedRoots = virtualParentSegments.Select(fun vps -> vps.Target)
                roots.Except(parentedRoots) 



            logInfo "Start F# Engine running..."
            activeCpu.BuildBitDependencies()
            activeCpu.Run()

            let subscriptions =
                [
                    for cpu in cpus do
                        Global.BitChangedSubject.Subscribe(onBitChanged cpu)


                    // OPC server 쪽에서 tag 값 변경시, 해당 tag 를 가지고 있는 모든 CPU 에 event 를 전달한다.
                    Global.TagChangeFromOpcServerSubject
                        .Subscribe(fun tc ->
                            cpus
                            |> Seq.filter(fun cpu -> cpu.TagsMap.ContainsKey(tc.TagName))
                            |> Seq.iter(fun cpu -> onOpcTagChanged cpu tc))

                    for cpu in cpus do
                        readTagsFromOpc cpu opc
                        runCpu cpu  // ! 실제 수행!!
                ]

            new CompositeDisposable(subscriptions)

