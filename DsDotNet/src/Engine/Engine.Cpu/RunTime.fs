namespace Engine.Cpu

open Engine.Core
open Dual.Common.Core.FS
open System
open System.Linq
open System.Collections.Generic
open System.Threading.Tasks
open System.Threading
open Engine.Core.TagKindModule
open System.Runtime.CompilerServices
open Engine.CodeGenCPU
open System.Reactive.Subjects
open System.Reactive.Disposables
open System.Reactive.Linq
open Engine.CodeGenHMI

[<AutoOpen>]
module RunTime =

    type DsCPU(css:CommentedStatement seq, mySystem:DsSystem, loadedSystems:DsSystem seq
             , hmiTags:IDictionary<string, TagWeb>) =

        let statements = css |> Seq.map(fun f -> f.Statement)
        let mapRungs = getRungMap(statements)
        let cpuStorages = mapRungs.Keys
        let tagStorages = mySystem.TagManager.Storages
        let stopBtn = (mySystem.TagManager :?> SystemManager).GetSystemTag(SystemTag.stop_btn)
        let systems = [mySystem] @ loadedSystems
        let mutable cts = new CancellationTokenSource()
        let mutable run:bool = false
        let mutable scanSimDelay:int = 10
        let disposables = new CompositeDisposable()

        let tagWebChangedFromCpuSubject = new Subject<TagWeb>()
        let tagWebChangedFromWebSubject = new Subject<TagWeb>()

        do
            let subscription =
                CpusEvent.ValueSubject.Subscribe(fun (_, stg, _) ->
                    //TagWeb 전송 대상만 이벤트 처리
                    if hmiTags.ContainsKey stg.Name   
                    then 
                        let tagWeb = hmiTags[stg.Name]
                        logDebug $"Server Updating TagWeb from CPU: {tagWeb.Name}:{tagWeb.KindDescription}={tagWeb.Value}"
                        tagWeb.SetValue(stg.BoxedValue)
                        tagWebChangedFromCpuSubject.OnNext(tagWeb)
                )
            disposables.Add subscription

            if RuntimeDS.Package.IsPackageSIM()
            then stopBtn.BoxedValue <- false
                 systems.Iter(fun sys-> cpuSimOn(sys))
                 systems.Iter(fun sys-> preAction(sys, true))


        let subscription = 
            tagWebChangedFromWebSubject.Subscribe(fun tagWeb-> 
                    logDebug $"Server Updating TagWeb from Web: {tagWeb.Name}:{tagWeb.KindDescription}={tagWeb.Value}"
                    let cpuTag = tagStorages.[tagWeb.Name]
                    cpuTag.BoxedValue <-tagWeb.Value
            )

        let scanOnce() = 
            //나머지 수식은 Changed Event가 있는것만 수행해줌
            let chTags = cpuStorages.ChangedTags()
            
            let exeStates = chTags.ExecutableStatements(mapRungs) 
            chTags.ChangedTagsClear(systems)

            chTags.Iter(notifyPreExcute)  // 상태보고/물리Out 처리
                  
            if exeStates.any() 
            then exeStates.Iter(fun s->s.Do())
          //       chTags.Iter(notifyPostExcute)  // HMI Forceoff 처리

            chTags


        let mutable ctsScan = new CancellationTokenSource()
        let asyncStart = 
            async { 
                        // 시스템 ON 및 값 변경이 없는 조건 수식은 관련 수식은 Changed Event가 없어서 한 번 수행해줌
                for s in statements do s.Do()
                        //timer, counter 제외 timer.DN, ctn.UP, ctn.DN 은 TagKind 부여해서 동작
                use _ = CpusEvent.ValueSubject
                                 .Where(fun (_, stg, _) -> stg.TagKind <> skipValueChangedForTagKind) 
                                 .Subscribe(fun _ -> if not(ctsScan.IsCancellationRequested) then ctsScan.Cancel())
                
                while run do
                    scanOnce() |> ignore 
                    try
                        if RuntimeDS.Package.IsPackageSIM()
                        then do! Async.Sleep(scanSimDelay)
                        else 
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

        let doStepByStatusAsync(activeSys) =
            for s in statements do s.Do() 
            task {
                let mutable endStepByStatus = false
                while not(endStepByStatus) do
                    let chTags = scanOnce()
                    endStepByStatus <- chTags.isEmpty() 
                                    || chTags.Where(fun f->f.DsSystem = activeSys)
                                             .Where(fun f-> f.IsStatusTag()).any()
            }

        

        do
            disposables.Add subscription
            ()

        interface IDisposable with
            member x.Dispose() = x.Dispose()

        ///MySystem + LoadedSystems
        member x.Systems = [x.MySystem] @ loadedSystems
        member x.MySystem = mySystem
        member x.LoadedSystems = loadedSystems
        member x.IsRunning = run
        member x.ScanSimDelay  with get() = scanSimDelay and set(v) = scanSimDelay <- v
        member x.CommentedStatements = css
        
        member x.Dispose() =
            x.Reset()
            disposables.Dispose()

        member x.Run()  = doRun()
        member x.RunInBackground()  = async { doRun() } |> Async.Start
    
        member x.Stop() =
            doScanStop()

        member x.Step() =
            doScanStop()
            scanOnce() |> ignore 

        member x.StepByStatusAsync(activeSys:DsSystem) = 
            doScanStop()
            doStepByStatusAsync(activeSys)

        member x.Reset() =
            doScanStop()
            syncReset(mySystem)
            scanOnce() |> ignore

        member x.QuickDriveReady() =
            systems.Iter(fun sys-> preAction(sys, true))
            scanOnce() |> ignore

        member x.TagWebChangedFromWebSubject = tagWebChangedFromWebSubject
        member x.TagWebChangedFromCpuSubject = tagWebChangedFromCpuSubject

    [<Extension>]
    type DsCpuExt  =
        //Job 만들기
        [<Extension>]
        static member GetDsCPU (dsSys:DsSystem) : DsCPU*HMIPackage =
            let loadedSystems = dsSys.GetRecursiveLoadedSystems()

            // Initialize storages and load CPU statements
            let storages = Storages()
            let pous = CpuLoaderExt.LoadStatements(dsSys, storages) |> Seq.toList

            // Create a list to hold commented statements
            let mutable css = []

            // Add commented statements from each CPU
            for cpu in pous do
                css <- css @ cpu.CommentedStatements() |> List.ofSeq
            let hmiPackage = ConvertHMIExt.GetHMIPackage(dsSys)   
            let hmiPackageTags = ConvertHMIExt.GetHMIPackageTags(hmiPackage)   
            // Create and return a DsCPU object
            new DsCPU(css, dsSys, loadedSystems, hmiPackageTags), hmiPackage

