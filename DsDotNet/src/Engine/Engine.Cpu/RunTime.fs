namespace Engine.Cpu

open Engine.Core
open Dual.Common.Core.FS
open System
open System.Linq
open System.Collections.Generic
open System.Threading.Tasks
open System.Threading
open System.Runtime.CompilerServices
open Engine.CodeGenCPU
open System.Reactive.Subjects
open System.Reactive.Disposables
open System.Reactive.Linq
open Engine.CodeGenHMI
open Engine.Core.MapperDataModule

[<AutoOpen>]
module RunTimeModule =

    type DsCPU(css:CommentedStatement seq, mySystem:DsSystem, loadedSystems:DsSystem seq
             , hmiTags:IDictionary<string, TagWeb>) =

        let statements = css |> Seq.map(fun f -> f.Statement) |> toArray
        let mapRungs = getRungMap(statements)
        let cpuStorages = mapRungs.Keys |> toArray
        let tagStorages = mySystem.TagManager.Storages
        let systems = [mySystem] @ loadedSystems |> toArray
        let mutable cts = new CancellationTokenSource()
        let mutable run:bool = false
        let disposables = new CompositeDisposable()

        let tagWebChangedFromCpuSubject = new Subject<TagWeb>()
        let tagWebChangedFromWebSubject = new Subject<TagWeb>()
        let tagChangedForIOHub = new Subject<IStorage seq>()

        do
            let subscription =
                CpusEvent.ValueSubject.Subscribe(fun (_, stg, _) ->
                    //TagWeb 전송 대상만 이벤트 처리
                    if hmiTags.ContainsKey stg.Name then
                        let tagWeb = hmiTags[stg.Name]
                        debugfn $"Server Updating TagWeb from CPU: {tagWeb.Name}:{tagWeb.KindDescription}={tagWeb.Value}"
                        tagWeb.SetValue(stg.BoxedValue)
                        tagWebChangedFromCpuSubject.OnNext(tagWeb)
                )
            disposables.Add subscription

            let subscription =
                tagWebChangedFromWebSubject.Subscribe(fun tagWeb->
                        debugfn $"Server Updating TagWeb from Web: {tagWeb.Name}:{tagWeb.KindDescription}={tagWeb.Value}"
                        let cpuTag = tagStorages.[tagWeb.Name]
                        cpuTag.BoxedValue <-tagWeb.Value
                )
            disposables.Add subscription

        let scanOnce() =
            //나머지 수식은 Changed Event가 있는것만 수행해줌
            let chTags = cpuStorages.GetChangedTags() |> toArray

            //Changed 있는것만 IO Hub로 전송
            if chTags.Any() then tagChangedForIOHub.OnNext chTags
            //ClearChangedTags 전에 exeStates 만들기
            let exeStates = chTags.ExecutableStatements(mapRungs)
            //ClearChangedTags
            chTags.ClearChangedTags(systems)

            // 상태보고/물리Out 처리 (주의 !! 연산에 관련된건만 이벤트 처리)
            chTags.Iter(notifyPreExcute)

            //exeStates Do 연산하기
            exeStates.Iter(fun s->s.Do())

            chTags

        let storages = mySystem.Storages
        let tagIndexSet =
            storages
            |> Seq.groupBy(fun f -> f.Value.DataType)
            |> Seq.collect(fun (k, v) -> v |> Seq.mapi(fun i s -> s.Value.Name, (k, i)))
            |> dict


        let mutable ctsScan = new CancellationTokenSource()
        let asyncStart =
            async {
                // 시스템 ON 및 값 변경이 없는 조건 수식은 관련 수식은 Changed Event가 없어서 한 번 수행해줌
                for s in statements do s.Do()

                //timer, counter 제외 timer.DN, ctn.UP, ctn.DN 은 TagKind 부여해서 동작
                use _ =
                    CpusEvent.ValueSubject
                        .Where(fun (_, stg, _) -> stg.TagKind <> skipValueChangedForTagKind)
                        .Subscribe(fun _ -> if not ctsScan.IsCancellationRequested then ctsScan.Cancel())

                while run do
                    scanOnce() |> ignore
                    try
                        //10초 딜레이 크게 의미없음 CpusEvent.ValueSubject 되면 빠져나옴
                        do! Async.AwaitTask(Task.Delay(10000, ctsScan.Token))
                    with
                    | :? TaskCanceledException -> ctsScan <- new CancellationTokenSource()
            }

        let doRun() =
            logInfo "--- Running CPU.."
            if not run then
                run <- true
                Async.StartImmediate(asyncStart, cts.Token) |> ignore

        let doScanStop() =
            logInfo "--- Stopping CPU.."
            cts.Cancel()
            cts <- new CancellationTokenSource()
            run <- false;

        let doStepByStatus(activeSys) =
            //for s in statements do s.Do()
            let mutable endStepByStatus = false
            while not(endStepByStatus) do
                let chTags = scanOnce()
                endStepByStatus <- chTags.IsEmpty()
                                   || chTags |> Seq.exists (fun f -> f.DsSystem = activeSys && f.IsStatusTag())

        interface IDisposable with
            member x.Dispose() = x.Dispose()

        ///MySystem + LoadedSystems
        member x.Systems = systems
        member x.MySystem = mySystem
        member x.Storages = storages
        member x.LoadedSystems = loadedSystems
        member x.IsRunning = run
        member x.CommentedStatements = css

        member x.Dispose() =
            x.Reset()
            disposables.Dispose()

        member x.Run()  = doRun()
        member x.Stop() = doScanStop()

        member x.Step() =
            doScanStop()
            scanOnce() |> ignore

        member x.StepByStatus() =
            doScanStop()
            doStepByStatus(mySystem)

        member x.Reset() =
            doScanStop()
            syncReset(mySystem)
            
            scanOnce() |> ignore


        member x.TagWebChangedFromWebSubject = tagWebChangedFromWebSubject
        member x.TagWebChangedFromCpuSubject = tagWebChangedFromCpuSubject
        member x.TagChangedForIOHub = tagChangedForIOHub
        member x.TagIndexSet = tagIndexSet

    type Runtime = DsCPU*HMIPackage*(PouGen[])

[<Extension>]
type DsCpuExt  =
    /// DsSystem 으로부터 Runtime 생성 : DsCPU*HMIPackage*(PouGen[])
    [<Extension>]
    static member CreateRuntime (dsSys:DsSystem) (modelCnf:ModelConfig): Runtime =
        RuntimeDS.ReplaceSystem dsSys
        RuntimeDS.UpdateParam(modelCnf.RuntimeMode, modelCnf.TimeSimutionMode)
        

        dsSys.GetCallVertices()
             .Where(fun f-> f.CallTime.IsDefault)
             .Iter(fun f-> f.CallTime.TimeOut <- Some modelCnf.TimeoutCall)

        let loadedSystems = dsSys.GetRecursiveLoadedSystems()

        // Initialize storages and create POU's for the system
        let storages = Storages()
        let pous = dsSys.GeneratePOUs storages modelCnf |> toArray

        modelCnf.TagConfig.UserMonitorTags.Iter(fun f->
            if  storages.ContainsKey(f.Name) then
                failwith $"UserTags {f.Name} 중복된 태그명"

            match tryTextToDataType(f.DataType) with
            | Some dataType ->
                if not (f.Address.IsNullOrEmpty()) then
                    let tag = dataType.ToType().CreateBridgeTag(f.Name, f.Address, dataType.DefaultValue(), MonitorTag.UserTagType|>int|>Some)
                    storages.Add(tag.Name, tag)

            | None -> failwith $"{f} 미지원 데이터 타입"
            )

        // Create commented statements from each POU's
        let css = pous.Collect(_.CommentedStatements())
        let hmiPackage = ConvertHMIExt.GetHMIPackage(dsSys, modelCnf.HwIP)
        let hmiPackageTags = ConvertHMIExt.GetHMIPackageTags(hmiPackage)
        // Create and return a DsCPU object
        new DsCPU(css, dsSys, loadedSystems, hmiPackageTags), hmiPackage, pous

