namespace Engine.Cpu

open Engine.Core
open Dual.Common.Core.FS
open System
open System.Collections.Generic
open System.Threading.Tasks
open System.Threading

[<AutoOpen>]
module RunTime =


    type DsCPU(css:CommentedStatement seq, sys:DsSystem, cpuMode:CpuRunMode) =
        let mapRungs = Dictionary<IStorage, HashSet<Statement>>()
        let statements = css |> Seq.map(fun f -> f.Statement)
        let mutable cts = new CancellationTokenSource()
        let mutable runSubsc:IDisposable = null
        let mutable run:bool = false
        let asyncStart = async { 
                            while run do   
                            for s in statements do s.Do() 
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
            Async.StartImmediate(getAsyncReset(statements, sys), cts.Token)

        member x.Dispose() =  
            cts.Cancel()
            runSubsc.Dispose()
