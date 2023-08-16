namespace Engine.Cpu

open Engine.Core
open Dual.Common.Core.FS
open System
open System.Linq
open System.Reactive.Linq
open System.Reactive.Disposables
open System.Collections.Generic
open System.Collections.Concurrent
open System.Threading.Tasks
open System.Threading

[<AutoOpen>]
module RunTime =
    type cpuRunMode =
    |Event
    |Scan

    type DsCPU(css:CommentedStatement seq, sys:DsSystem, cpuMode:cpuRunMode) =
        let mapRungs = ConcurrentDictionary<IStorage, HashSet<Statement>>()
        let statements = css |> Seq.map(fun f -> f.Statement)
        let mutable cts = new CancellationTokenSource()
       

        let runSubscribe() =
            let subscribe =
                CpusEvent.ValueSubject      //cpu 단위로 이벤트 필요 ahn
                //자신 CPU와 같은 시스템 또는 참조시스템만 연산처리
                 .Where(fun (system, _storage, _value) -> system = sys || sys.ReferenceSystems.Contains(system:?> DsSystem))
                 .Subscribe(fun (system, storage, _value) ->
                    //Step 1 상태 UI 업데이트
                    system.NotifyStatus(storage);
                    //Step 2 관련수식 연산

                    if cpuMode = Event
                    then 
                        if mapRungs.ContainsKey storage
                        then
                            for f in mapRungs[storage]
                                do 
                                async { f.Do()}|> Async.StartImmediate
                                        
                            //mapRungs[storage]
                            //|> Seq.map (fun f-> async { f.Do() } )
                            //|> Async.Parallel
                            //|> Async.Ignore
                            //|> Async.RunSynchronously
                    )
            subscribe

        let mutable runSubsc:IDisposable = null
        let mutable run:bool = false

        let simPreAction() =  
            let simTags = 
                sys.TagManager.Storages
                   .Where(fun w-> 
                                w.Value.TagKind = (int)SystemTag.auto
                                ||   w.Value.TagKind = (int)SystemTag.drive
                                ||   w.Value.TagKind = (int)SystemTag.ready
                                ||   w.Value.TagKind = (int)SystemTag.sim
                        )
            simTags.Iter(fun t -> t.Value.BoxedValue <-  true) 
        do
            let total =
                [ for s in statements do
                    yield! s.GetSourceStorages()
                    yield! s.GetTargetStorages()
                ].Distinct()
            for item in total do
                mapRungs.TryAdd (item, HashSet<Statement>())|> verifyM $"Duplicated [{item.ToText()}]"
            let dicSource =
                statements
                    .Select(fun s -> s, s.GetSourceStorages())
                    |> dict |> Dictionary

            for rung in mapRungs do
                let sts = dicSource.Filter(fun f->f.Value.Contains(rung.Key))
                for st in sts do
                    rung.Value.Add(st.Key) |> verifyM $"Duplicated [{ st.Key.ToText()}]"

            runSubsc <- runSubscribe()

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
                simPreAction()
                if cpuMode = Scan
                then 
                    let t = async {
                            while run do                             
                                for s in statements do s.Do() 
                                //do! Async.Sleep(0)  //시스템 병렬 개별 Task 실행으로 필요 없음
                            }
                    Async.StartImmediate(t, cts.Token) |> ignore
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
            let t = async {
                let stgs =  sys.TagManager.Storages
                               .Where(fun w-> w.Value.TagKind <> (int)SystemTag.on)
                for tag in stgs do
                    let stg = tag.Value
                    match stg with
                    | :? TimerCounterBaseStruct as tc ->
                        tc.Clear()  // 타이머 카운터 리셋
                    | _ ->
                        stg.BoxedValue <- textToDataType(stg.DataType.Name).DefaultValue()
                //조건 1번 평가 (for : Ready State 이벤트)
                for s in statements do s.Do() 
            }
            Async.StartImmediate(t, cts.Token)


        member x.Dispose() =  
            cts.Cancel()
            runSubsc.Dispose()
