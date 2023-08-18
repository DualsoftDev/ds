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


    type DsCPU(css:CommentedStatement seq, sys:DsSystem, cpuMode:CpuRunMode) =
        let mapRungs = Dictionary<IStorage, HashSet<Statement>>()
        let statements = css |> Seq.map(fun f -> f.Statement)
        let systemOn =  sys.TagManager.Storages
                           .First(fun w-> w.Value.TagKind = (int)SystemTag.on).Value
                           
        let mutable cts = new CancellationTokenSource()
        let mutable runSubsc:IDisposable = null
        let mutable run:bool = false
        let asyncStart = 
            async { 
                //시스템 ON 및 값변경이 없는 조건 수식은  관련 수식은 Changed Event가 없어서한번 수행해줌
                for s in statements do s.Do() 

                //나머지 수식은 Changed Event가 있는것만 수행해줌
                while run do   

                    let tags = mapRungs.Keys |> Seq.where(fun (stg) -> stg.TagChanged)
                    let doSrcs = tags |> Seq.collect(fun stg -> mapRungs[stg]) |> Seq.toList
                        
                    if doSrcs.any() 
                    then
                        
                        //assert(tags.any(fun t->t.DsSystem <> sys))
                        //if tags.any(fun f-> f.Name.Contains("SensorRET") )
                        //then  
                        //    let a = tags.any();
                        //    ()
                        tags.Where(fun w->w.DsSystem = sys)         //자신 시스템에서만 TagChanged  <- false 가능
                        |> Seq.iter(fun (stg) -> stg.TagChanged <- false)   //Do 처리 수식 구한후 Changed 초기화
                        doSrcs |> Seq.iter(fun s -> s.Do())
            }
     
        do
            if cpuMode <> CpuRunMode.Non
            then 
                updateRungMap(statements, mapRungs)
                runSubsc <- runSubscribe(mapRungs, sys, cpuMode)

        //강제 전체 연산 임시 test용
        member x.ScanOnce() =
            let scanTask = async {
                    for s in statements do //cts.Token 의해서 멈춤
                    s.Do() }
            Async.StartAsTask(scanTask, TaskCreationOptions.None, cts.Token) 
            |> Async.AwaitTask

        member x.IsRunning = run
        member x.System = sys
        member x.CommentedStatements = css

        member x.Run() =
            if not <| run then 
                run <- true
                simPreAction(sys)

                if cpuMode = Scan
                then 
                    Async.StartImmediate(asyncStart, cts.Token) |> ignore
                else
                    x.ScanOnce()|> ignore // _ON 이벤트 없는 조건때문에 한번 스켄 수행

        member x.Stop() =
            cts.Cancel()
            cts <- new CancellationTokenSource() 
            run <- false;

        member x.Step() =
            x.Stop()
            x.ScanOnce() |> ignore

        member x.Reset() =
            x.Stop()
            Async.StartImmediate(getAsyncReset(statements, sys, systemOn), cts.Token)

        member x.Dispose() =  
            cts.Cancel()
            runSubsc.Dispose()
