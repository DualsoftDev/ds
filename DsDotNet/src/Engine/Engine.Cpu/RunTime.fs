namespace Engine.Cpu

open Engine.Core
open Dual.Common.Core.FS
open System
open System.Linq
open System.Collections.Generic
open System.Threading.Tasks
open System.Threading
open Engine.Core.TagKindModule

[<AutoOpen>]
module RunTime =


    type DsCPU(css:CommentedStatement seq, systems:DsSystem seq, cpuMode:RuntimePackage) =
        let statements = css |> Seq.map(fun f -> f.Statement)
        let mapRungs = getRungMap(statements)
        let cpuStorages = mapRungs.Keys
     
        let mutable cts = new CancellationTokenSource()
        let mutable run:bool = false

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
            systems.Iter(fun sys-> cpuModeToggle(sys, cpuMode))
            
            
            if not run then 
                run <- true
                Async.StartImmediate(asyncStart, cts.Token) |> ignore

        let doStop() = 
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

        member x.Systems = systems
        member x.IsRunning = run
        member x.CommentedStatements = css
        
        member x.Dispose() = doStop()
        member x.Run()  = doRun()
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

