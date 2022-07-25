namespace Engine.Runner

open System
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
            logInfo "Start F# Engine running..."
            /// OPC Server 에서 Cpu 가 가지고 있는 tag 값들을 읽어 들임
            /// Engine 최초 구동 시, 수행됨.
            let readTagsFromOpc (cpu:Cpu) (opc:OpcBroker) =
                let tpls = opc.ReadTags(cpu.TagsMap.map(fun t -> t.Key))
                for tName, value in tpls do
                    let tag = cpu.TagsMap[tName]
                    if tag.Value <> value then
                        onOpcTagChanged cpu (new OpcTagChange(tName, value))


            let subscriptions =
                [
                    for cpu in cpus do
                        Global.BitChangedSubject.Subscribe(onBitChanged cpu)


                    // OPC server 쪽에서 tag 값 변경시, 해당 tag 를 가지고 있는 모든 CPU 에 event 를 전달한다.
                    Global.OpcTagChangedSubject
                        .Subscribe(fun tc ->
                            cpus
                            |> Seq.filter(fun cpu -> cpu.TagsMap.ContainsKey(tc.TagName))
                            |> Seq.iter(fun cpu -> onOpcTagChanged cpu tc))

                    for cpu in cpus do
                        readTagsFromOpc cpu opc
                        runCpu cpu
                ]

            new CompositeDisposable(subscriptions)

