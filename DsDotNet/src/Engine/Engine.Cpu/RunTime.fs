namespace Engine.Cpu

open Engine.Core
open Dual.Common.Core.FS
open System
open System.Linq
open System.Collections.Generic
open System.Threading.Tasks
open System.Threading
[<AutoOpen>]
module RunTime =


    type DsCPU(css:CommentedStatement seq, systems:DsSystem seq, cpuMode:RuntimePackage) =
        let mapRungs = Dictionary<IStorage, HashSet<Statement>>()
        let cpuStorages = mapRungs.Keys
        let statements = css |> Seq.map(fun f -> f.Statement)
     
        let mutable cts = new CancellationTokenSource()
        let mutable run:bool = false
        let asyncStart = 
            async { 
                //시스템 ON 및 값변경이 없는 조건 수식은  관련 수식은 Changed Event가 없어서한번 수행해줌
                for s in statements do s.Do() 
                //나머지 수식은 Changed Event가 있는것만 수행해줌
                while run do   

                    let chTags = cpuStorages.ChangedTags()
                    let states = chTags.ExecutableStatements(mapRungs) 
                  
                    if states.any()  
                    then
                        chTags.ChangedTagsClear(systems)
                        chTags.Iter(fun f-> 
                            f.DsSystem.NotifyStatus(f) //상태보고
                            f.DsSystem.NotifyHwOutput(f) //물리 Out 보고
                                            ) 
                        states.Iter(fun f->  f.Do()  )

            }
        do 
            updateRungMap(statements, mapRungs)


        member x.Systems = systems
        member x.IsRunning = run
        member x.CommentedStatements = css
        
        member x.Run() =
            if not <| run then 
                systems.Iter(fun sys-> preAction(sys, cpuMode))
                run <- true
                Async.StartImmediate(asyncStart, cts.Token) |> ignore

        member x.Stop() =
            cts.Cancel()
            cts <- new CancellationTokenSource() 
            run <- false;

        member x.Step() =
            x.Stop()
            systems.Iter(fun sys-> preAction(sys, cpuMode))
            singleScan(statements, systems)

        member x.Reset() =
            x.Stop()
            syncReset(statements, systems, false);
        member x.ResetActive() =
            x.Stop()
            syncReset(statements, systems, true);

        member x.Dispose() =  
            cts.Cancel()
            //runSubsc.Dispose()
