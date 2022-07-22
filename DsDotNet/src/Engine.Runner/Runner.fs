namespace Engine.Runner

open System
open System.Reactive.Linq
open System.Runtime.CompilerServices
open System.Reactive.Disposables

open Engine.Core
open Dual.Common

[<AutoOpen>]
module ModelRunnerModule =
    let runCpu (cpu:Cpu) = ()

    type ModelRunner(model:Model) as this =
        let mutable _disposabls = new CompositeDisposable()
        let cpus = model.Cpus
        let activeCpu = cpus |> Seq.filter(fun cpu -> cpu.IsActive) |> Seq.exactlyOne
        let otherCpus = cpus |> Seq.except([activeCpu])

        interface IDisposable with
            member x.Dispose() = x.Dispose()

        member private x.Dispose() =
            _disposabls.Dispose()
            _disposabls <- new CompositeDisposable()
        member val Model = model

        member x.Run() =
            x.Dispose()

            [
                for cpu in model.Cpus do
                    Global.BitChangedSubject.Subscribe(fun bc -> cpu.OnBitChanged(bc))


                // OPC server 쪽에서 tag 값 변경시, 해당 tag 를 가지고 있는 모든 CPU 에 event 를 전달한다.
                Global.OpcTagChangedSubject
                    .Subscribe(fun tc ->
                        cpus
                        |> Seq.filter(fun cpu -> cpu.TagsMap.ContainsKey(tc.TagName))
                        |> Seq.iter(fun cpu -> cpu.OnOpcTagChanged(tc)))

            ]|> List.iter _disposabls.Add

            cpus |> Seq.iter runCpu
            this


    let run (model:Model) =
        logInfo "Start running model.."
        let runner = new ModelRunner(model)
        runner.Run()


[<Extension>]
type ModelExtension =
    /// Run model
    [<Extension>] static member Run(model:Model) = run model
    //[<Extension>] static member Run(cpu:Cpu) = runCpu cpu
