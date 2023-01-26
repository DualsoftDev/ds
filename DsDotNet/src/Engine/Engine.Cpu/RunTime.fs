namespace Engine.Cpu

open Engine.Core
open Engine.Common.FS
open System
open System.Linq
open System.Collections.Generic
open System.Collections.Concurrent

[<AutoOpen>]
module RunTime =
    type DsCPU(css:CommentedStatement seq, sys:DsSystem) =
        let mapRungs = ConcurrentDictionary<IStorage, HashSet<Statement>>()
        let statements = css |> Seq.map(fun f -> f.Statement)
        let runSubscribe() =
            let subscribe =
                sys.ValueChangeSubject      //cpu 단위로 이벤트 필요 ahn
                 .Subscribe(fun (storage, newValue_) ->
                    //Step 1 상태보고
                    match storage with
                    | :? PlanVar<bool> as p -> if p.Value then p.NotifyStatus()
                    | :? BridgeTag<bool> as a -> ()//hmi ?
                    | _ -> ()


                    //Step 2 관련수식 연산
                    if mapRungs.ContainsKey storage
                    then
                        for statement in mapRungs[storage] do
                            statement.Do()
                    //    async {statement.Do()}|> Async.StartImmediate
                    else
                        let mapRungs = mapRungs
                        failwithlog "Error runSubscribe"  //디버깅후 예외 처리
                    )
            subscribe

        let mutable runSubscription:IDisposable = null

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

        member x.System = sys
        member x.ToTextStatement() =
            let statementTexts = statements.Select(fun statement -> statement.ToText())
            String.Join("\r\n", statementTexts)
