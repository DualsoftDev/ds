namespace Engine.Cpu

open Engine.Core
open System
open System.Linq
open System.Collections.Generic
open System.Collections.Concurrent

[<AutoOpen>]
module RunTime =
    type DsCPU(css:CommentedStatement seq) =
        let mapRungs = ConcurrentDictionary<IStorage, HashSet<Statement>>()
        let statements = css |> Seq.map(fun f -> f.Statement)
        let runSubscribe() =
            let subscribe =
                ValueSubject
                 .Subscribe(fun evt ->
                    //Step 1 상태보고
                    match evt with
                    | :? PlanTag<bool> as p -> p.NotifyStatus()
                    | :? ActionTag<bool> as a -> ()//hmi ?
                    | _ -> ()


                    //Step 2 관련수식 연산
                    if mapRungs.ContainsKey evt
                    then
                        for statement in mapRungs[evt] do
                       //     statement.Do()
                        async {statement.Do()}|> Async.StartImmediate
                    else
                        let mapRungs = mapRungs
                        ()//failwithlog "Error"  //디버깅후 예외 처리
                    )
            subscribe

        let mutable runSubscription:IDisposable = null

        do
            let usedItems =
                [ for s in statements do
                    yield! s.GetSourceStorages()
                    yield! s.GetTargetStorages()
                ] |> Seq.distinct

            let dicSource =
                statements
                    .Select(fun s -> s, s.GetSourceStorages())
                    |> dict |> Dictionary

            for item in usedItems do
            for s in statements do
                if dicSource[s].Contains item then
                    if mapRungs.ContainsKey item then
                        mapRungs[item].Add s |> verifyM $"Duplicated [{s.ToText()}]"
                    else
                        mapRungs.TryAdd(item, [s]|>HashSet) |> verifyM $"Duplicated [{item.ToText()}]"


        //강제 전체 연산 임시 test용
        member x.ScanOnce() =
            for s in statements do
                s.Do()

            ///running 이 Some 이면 Expression 처리 동작 중
        member x.IsRunning = runSubscription <> null
        member x.CommentedStatements = css
        member x.Run() =
            assert(runSubscription = null)
            runSubscription <- runSubscribe()
        member x.Stop() =
            assert(runSubscription <> null)
            runSubscription.Dispose()
            runSubscription <- null

        member x.ToTextStatement() =
            let statementTexts = statements.Select(fun statement -> statement.ToText())
            String.Join("\r\n", statementTexts)
