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
                 .Subscribe(fun (storage, _newValue) ->
                    //for UI
                    sys.NotifyValue(storage, _newValue);
                    sys.NotifyStatus(storage);
                    let doExpr(statement:Statement) =
                            let endEvent = storage.IsEndThread()
                            if endEvent
                            then
                                async {
                                    do! Async.Sleep(1000)
                                    statement.Do() }
                                    |> Async.StartImmediate
                            else
                                //statement.Do()
                                //debugging  sleep
                                async {
                                    do! Async.Sleep(10)
                                    statement.Do()
                                    }|> Async.RunSynchronously
                                //debugging  sleep


                    //Step 1 관련수식 연산
                    if mapRungs.ContainsKey storage
                    then
                        for statement in mapRungs[storage] do
                            doExpr(statement)
                            match statement with
                            | DuAssign (_expr, (:? RisingCoil  as rc)) ->
                                if rc.HistoryFlag.LastValue = true
                                then
                                    for pulseStatement in mapRungs[rc.Storage] do
                                        doExpr(pulseStatement)

                            | DuAssign (_expr, (:? FallingCoil as fc)) ->
                                if fc.HistoryFlag.LastValue = false
                                then
                                    for pulseStatement in mapRungs[fc.Storage] do
                                        doExpr(pulseStatement)

                            | _->  ()
                    else
                        failwithlog $"Error {getFuncName()} : {storage.Name}"  //디버깅후 예외 처리
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
            if not <| x.IsRunning then
                runSubscription <- runSubscribe()
        member x.Stop() =
            if x.IsRunning then
                runSubscription.Dispose()
                runSubscription <- null

        member x.Dispose() =  x.Stop()
        member x.System = sys
