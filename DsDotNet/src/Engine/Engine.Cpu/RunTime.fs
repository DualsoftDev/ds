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

[<AutoOpen>]
module RunTime =

    type DsCPU(css:CommentedStatement seq, mySystem:DsSystem, loadedSystems:DsSystem seq, cpuMode:RuntimePackage) =
        let statements = css |> Seq.map(fun f -> f.Statement)
        let mapRungs = getRungMap(statements)
        let cpuStorages = mapRungs.Keys
        let systems = [mySystem] @ loadedSystems
        let mutable cts = new CancellationTokenSource()
        let mutable run:bool = false

        let tagWebChangedSubject = new Subject<TagWeb>()

        let scanOnce() = 
            //나머지 수식은 Changed Event가 있는것만 수행해줌
            let chTags = cpuStorages.ChangedTags()
            let exeStates = chTags.ExecutableStatements(mapRungs) 
            chTags.ChangedTagsClear(systems)

            chTags.Iter(notifyPreExcute)  // 상태보고/물리Out 처리
                  
            if exeStates.any() then exeStates.Iter(fun s->s.Do())

            chTags.Iter(notifyPostExcute)  // HMI Forceoff 처리
            chTags

        let asyncStart = 
            async { 
                //시스템 ON 및 값변경이 없는 조건 수식은  관련 수식은 Changed Event가 없어서한번 수행해줌
                for s in statements do s.Do() 
                while run do   
                    scanOnce() |> ignore
            }

        let doRun() = 
            logInfo "--- Running CPU.."
            systems.Iter(fun sys-> cpuModeToggle(sys, cpuMode))
            
            
            if not run then 
                run <- true
                Async.StartImmediate(asyncStart, cts.Token) |> ignore

        let doStop() = 
            logInfo "--- Stopping CPU.."
            cts.Cancel()
            cts <- new CancellationTokenSource() 
            run <- false;

        let doStepByStatusAsync(activeSys) =
            task {
                let mutable endStepByStatus = false
                while not(endStepByStatus) do
                    let chTags = scanOnce()
                    endStepByStatus <- chTags.isEmpty() 
                                    || chTags.Where(fun f->f.DsSystem = activeSys)
                                             .Where(fun f->f.IsStatusTag()).any()
            }

        do 
            ()

        interface IDisposable with
            member x.Dispose() = x.Dispose()

        ///MySystem + LoadedSystems
        member x.Systems = [x.MySystem] @ loadedSystems
        member x.MySystem = mySystem
        member x.LoadedSystems = loadedSystems
        member x.IsRunning = run
        member x.CommentedStatements = css
        
        

        member x.Dispose() = doStop()
        member x.Run()  = doRun()
        member x.RunInBackground()  = async { doRun() } |> Async.Start
        member x.AutoDriveSetting()  =          
            systems.Iter(fun sys-> preAction(sys, cpuMode, true))

        member x.Stop() = doStop()

        member x.Step() =
            doStop()
            scanOnce()

        member x.StepByStatusAsync(activeSys:DsSystem) = 
            doStop()
            doStepByStatusAsync(activeSys)

        member x.Reset() =
            doStop()
            syncReset(systems, false)
            scanOnce()

        // todo: 함수 작성.  실패시 실패 이유 반환, 성공시 null 문자열 반환
        member x.UpdateTagWeb(tagWeb:TagWeb): ErrorMessage =
            logDebug "Server Updating TagWeb"
            tagWebChangedSubject.OnNext(tagWeb)
            null

        // todo: TagWeb 변경시 이벤트 발생
        member x.TagWebChangedSubject = tagWebChangedSubject

                

    [<Extension>]
    type DsCpuExt  =
        //Job 만들기
        [<Extension>]
        static member GetDsCPU (dsSys:DsSystem, runtimePackage:RuntimePackage) : DsCPU =
            let loadedSystems = dsSys.GetRecursiveLoadedSystems()

            // Initialize storages and load CPU statements
            let storages = Storages()
            let pous = CpuLoaderExt.LoadStatements(dsSys, storages) |> Seq.toList

            // Create a list to hold commented statements
            let mutable css = []

            // Add commented statements from each CPU
            for cpu in pous do
                css <- css @ cpu.CommentedStatements() |> List.ofSeq

            // Create and return a DsCPU object
            new DsCPU(css, dsSys, loadedSystems, runtimePackage)

