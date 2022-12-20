namespace Engine.Cpu

open Engine.Core
open System
open System.Linq
open Engine.Parser.FS
open System.Collections.Generic
open System.Collections.Concurrent

[<AutoOpen>]
module RunTime =
    type DsCPU(storages:Storages, text:string, statements:Statement seq) =
        let mapRungs = ConcurrentDictionary<IStorage, HashSet<Statement>>()
        let statements = statements |> List.ofSeq
        let runSubscribe() =
            let subscribe =
                ValueSubject
                 .Subscribe(fun evt ->
                    //Step 1 상태보고
                    match evt with
                    | :? DsBit as b -> b.NotifyStatus()
                    | _ -> ()

                    //Step 2 관련수식 연산
                    if mapRungs.ContainsKey evt
                    then
                        for statement in mapRungs[evt] do
                        async {statement.Do()}|> Async.StartImmediate
                    else
                        let mapRungs = mapRungs
                        ()//failwith "Error"  //디버깅후 예외 처리
                    )
            subscribe

        let mutable runningSubscription:IDisposable = null

        let statements =
            if String.IsNullOrEmpty text then
                statements
            else
                (text |> parseCode storages) @ statements

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
        member x.Scan() =
            for s in statements do
                s.Do()

            ///running 이 Some 이면 Expression 처리 동작 중
        member x.IsRunning = runningSubscription <> null
        member x.Run() =
            assert(runningSubscription = null)
            runningSubscription <- runSubscribe()
        member x.Stop() =
            assert(runningSubscription <> null)
            runningSubscription.Dispose()
            runningSubscription <- null

        member x.ToTextStatement() =
            let statementTexts = statements.Select(fun statement -> statement.ToText())
            String.Join("\r\n", statementTexts)
