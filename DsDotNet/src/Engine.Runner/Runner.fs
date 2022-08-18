namespace Engine.Runner

open System
open Engine.Core

[<AutoOpen>]
module ModelRunnerModule =
    let Initialize() =
        SegmentBase.Create <-
            new Func<string, RootFlow, SegmentBase>(
                fun name (rootFlow:RootFlow) ->
                    let seg = FsSegment(rootFlow.Cpu, name)
                    seg.ContainerFlow <- rootFlow
                    rootFlow.AddChildVertex(seg)
                    seg)
    ()



    //let runCpu (cpu:Cpu) = ()

    //type EngineRunner(engine:Engine) as this =
    //    let mutable _disposabls = new CompositeDisposable()
    //    let model = engine.Model
    //    let cpus = model.Cpus
    //    let activeCpu = cpus |> Seq.filter(fun cpu -> cpu.IsActive) |> Seq.exactlyOne
    //    let otherCpus = cpus |> Seq.except([activeCpu])

    //    interface IDisposable with
    //        member x.Dispose() = x.Dispose()

    //    member private x.Dispose() =
    //        _disposabls.Dispose()
    //        _disposabls <- new CompositeDisposable()
    //    member val Model = model

    //    member x.Run() =
    //        x.Dispose()

    //        [
    //            for cpu in model.Cpus do
    //                Global.BitChangedSubject.Subscribe(fun bc -> cpu.OnBitChanged(bc))


    //            // OPC server 쪽에서 tag 값 변경시, 해당 tag 를 가지고 있는 모든 CPU 에 event 를 전달한다.
    //            Global.OpcTagChangedSubject
    //                .Subscribe(fun tc ->
    //                    cpus
    //                    |> Seq.filter(fun cpu -> cpu.TagsMap.ContainsKey(tc.TagName))
    //                    |> Seq.iter(fun cpu -> cpu.OnOpcTagChanged(tc)))

    //        ]|> List.iter _disposabls.Add

    //        cpus |> Seq.iter runCpu
    //        this


    //let run (engine:Engine) =
    //    logInfo "Start running model.."
    //    let runner = new EngineRunner(engine)
    //    runner.Run()


//[<Extension>]
//type ModelExtension =
//    /// Run model
//    [<Extension>] static member Run(model:Engine) = run model
//    //[<Extension>] static member Run(cpu:Cpu) = runCpu cpu
