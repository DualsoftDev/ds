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
        member x.Model = model
        member x.Opc = opc
        member x.Cpu = activeCpu


        member x.Run() =
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

