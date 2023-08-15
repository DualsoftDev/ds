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

[<AutoOpen>]
module RunTime =
    type DsCPU(css:CommentedStatement seq, sys:DsSystem) =
        let mapRungs = ConcurrentDictionary<IStorage, HashSet<Statement>>()
        let statements = css |> Seq.map(fun f -> f.Statement)
        let runSubscribe() =
            let subscribe =
                CpusEvent.ValueSubject      //cpu 단위로 이벤트 필요 ahn
                //자신 CPU와 같은 시스템 또는 참조시스템만 연산처리
                 .Where(fun (system, _storage, _value) -> system = sys || sys.ReferenceSystems.Contains(system:?> DsSystem))
                 .Subscribe(fun (system, storage, _value) ->
                        //Step 1 상태 UI 업데이트
                        system.NotifyStatus(storage);
                        //Step 2 관련수식 연산
                        //if mapRungs.ContainsKey storage
                        //then
                        //    mapRungs[storage]
                        //    |> Seq.map (fun f-> async { f.Do() } )
                        //    |> Async.Parallel
                        //    |> Async.Ignore
                        //    |> Async.RunSynchronously


                        //else
                        //    ()
                            //failwithlog $"Error {getFuncName()} : {storage.Name}"  //디버깅후 예외 처리

                            //for statement in mapRungs[storage] do
                            //    if storage.IsStartThread()
                            //    then
                            //        //statement.Do()
                            //        async {
                            //            do! Async.Sleep(200)
                            //            statement.Do() }
                            //            |> Async.StartImmediate
                            //    else
                            //        statement.Do()

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
                async {
                        while x.IsRunning
                            do x.ScanOnce()
                               do! Async.Sleep(50)
                }|> Async.StartImmediate

        member x.Stop() =
            if x.IsRunning then
                runSubscription.Dispose()
                runSubscription <- null

        member x.Step() =
            x.Stop()
            runSubscription <- runSubscribe()
            x.ScanOnce()
            x.Stop()

        member x.Reset() =
            x.Stop()
            sys.TagManager
               .Storages.Where(fun w-> w.Value.TagKind <> (int)SystemTag.on)
                        .Iter(fun s->
                            let stg = s.Value
                            match stg with
                            | :? TimerCounterBaseStruct as tc ->
                                tc.Clear()  // 타이머 카운터 리셋
                            | _ ->
                                stg.BoxedValue <- textToDataType(stg.DataType.Name).DefaultValue()
                )


        member x.Dispose() =  x.Stop()
        member x.System = sys
