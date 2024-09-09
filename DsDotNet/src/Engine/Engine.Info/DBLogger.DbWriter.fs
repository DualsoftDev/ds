namespace Engine.Info
open System
open System.Linq
open System.Threading.Tasks
open System.Collections.Concurrent
open System.Collections.Generic
open System.Reactive.Disposables
open Dapper

open Dual.Common.Core.FS
open Dual.Common.Db
open DBLoggerORM
open Engine.Core
open Engine.CodeGenCPU

[<AutoOpen>]
module DBWriterModule =
    /// DbReader 및 DbWriter 의 공통 조상 class
    [<AbstractClass>]
    type DbHandler(queryCriteria:QueryCriteria, logSet:LogSet option) as this =
        do
            DbHandler.TheDbHandler <- this
        static member val TheDbHandler = getNull<DbHandler>() with get, set
        interface IDisposable with
            member x.Dispose() = x.Dispose()
        member val Disposables = new CompositeDisposable() with get, set
        member val LogSet = logSet with get, set
        member x.CommonAppSettings = queryCriteria.CommonAppSettings
        abstract member Dispose: unit -> unit
        default x.Dispose() =
            tracefn "------------------ DbHandler disposing.."
            x.Disposables.Dispose()
            x.Disposables <- new CompositeDisposable()

    /// DB log writer.  Runtime engine
    type DbWriter private (queryCriteria:QueryCriteria, logSet) =
        inherit DbHandler(queryCriteria, logSet)
        let queue = ConcurrentQueue<ORMLog>()
        let originalToken2TokenIdDic = Dictionary<uint, int>()

        private new (queryCriteria) = new DbWriter(queryCriteria, None)

        static member val TheDbWriter = getNull<DbWriter>() with get, set


        //시뮬레이션 초기화 시에만 사용
        member x.ClearToken() = originalToken2TokenIdDic.Clear()
        member x.GetTokenId(originalToken:uint) = originalToken2TokenIdDic[originalToken]
        /// [Token] table 에 row 를 삽입하고, 해당 row 의 id 값을 반환
        member x.AllocateTokenId(originalToken:uint, at:DateTime) =
            assert(!! originalToken2TokenIdDic.ContainsKey(originalToken))

            use conn = x.CommonAppSettings.CreateConnection()
            let tokenId =
                conn.InsertAndQueryLastRowIdAsync(
                    null,
                    $"INSERT INTO {Tn.Token} (at, originalToken, modelId) VALUES (@At, @OriginalToken, @ModelId)",
                    {|At = at; OriginalToken=originalToken; ModelId=x.CommonAppSettings.LoggerDBSettings.ModelId|}).Result

            originalToken2TokenIdDic.Add(originalToken, tokenId) |> ignore
            tokenId

        /// branchToken 이 trunkToken 에 병합되는 event 를 db 에 기록
        member x.OnTokenMerged(branchToken, trunkToken) =
            let bTokenId, tTokenId = x.GetTokenId(branchToken), x.GetTokenId(trunkToken)
            use conn = x.CommonAppSettings.CreateConnection()
            conn.Execute($"UPDATE {Tn.Token} SET mergedTokenId = {tTokenId} WHERE id = {bTokenId}")



        /// 주기적으로 memory -> DB 로 log 를 write
        member x.dequeAndWriteDBAsync (nPeriod: int64) =
            verify (x.LogSet.Value.ReaderWriterType.HasFlag(DBLoggerType.Writer))

            // 큐를 배열로 바꾸고 비우는 함수
            let drainQueueToArray (queue: ConcurrentQueue<'T>) : 'T seq =
                let results = new ResizeArray<'T>()

                // 큐가 빌 때까지 반복해서 항목을 Dequeue
                let rec drainQueue () =
                    match queue.TryDequeue() with
                    | true, item ->
                        results.Add(item)
                        drainQueue ()
                    | false, _ -> () // 큐가 비었으므로 종료

                // 큐 드레이닝 시작
                drainQueue ()

                results

            task {
                let newLogs = drainQueueToArray (queue)

                if newLogs.any () then
                    logDebug $"{DateTime.Now}: Writing {newLogs.length ()} new logs."
                    use conn = x.CommonAppSettings.CreateConnection()
                    use! tr = conn.BeginTransactionAsync()

                    if (x.LogSet.Value.ReaderWriterType.HasFlag(DBLoggerType.Reader)) then
                        let newLogs = newLogs |> map (ormLog2Log x.LogSet.Value) |> toList
                        x.LogSet.Value.BuildIncremental newLogs
                    let currentModelId = x.CommonAppSettings.LoggerDBSettings.ModelId
                    for l in newLogs do
                        let! stg = conn.QueryFirstAsync<ORMStorage>($"SELECT * FROM [{Tn.Storage}] WHERE id = {l.StorageId}", tr)
                        let modelId = stg.ModelId
//#if DEBUG
//                        // currentModelId 와 log 의 modelId 는 서로 다를 수 있다.  모델 변경 직후, 예전 log 가 날아오는 경우 존재
//                        assert(stg.ModelId = modelId)
//                        assert (ORMDBSkeleton4Debug.Model.IsNone || ORMDBSkeleton4Debug.Model.Value.Id = modelId)
//                        //assert(ORMDBSkeleton.Storages[l.StorageId].ModelId = modelId)
//#endif
                        let query =
                            $"""INSERT INTO [{Tn.Log}]
                                (at, storageId, value, modelId, tokenId)
                                VALUES (@At, @StorageId, @Value, @ModelId, @TokenId)
                            """

                        let! _ =
                            conn.ExecuteAsync(
                                query,
                                {| At = l.At
                                   StorageId = l.StorageId
                                   Value = l.Value
                                   ModelId = modelId
                                   TokenId = l.TokenId |},
                                tr
                            )
                        ()

                    do! tr.CommitAsync()
            }

        member x.writePeriodicAsync = x.dequeAndWriteDBAsync


        member x.createLogSetForWriterAsync (queryCriteria: QueryCriteria) (systems: DsSystem seq) : Task<LogSet> =
            task {
                let commonAppSettings = queryCriteria.CommonAppSettings

                let! dbSckeleton = ORMDBSkeletonDTOExt.CreateLoggerDBAsync(queryCriteria.ModelId, $"Data Source={commonAppSettings.LoggerDBSettings.ConnectionPath}")
                ORMDBSkeleton4Debug <- dbSckeleton

                use conn = commonAppSettings.CreateConnection()
                use! tr = conn.BeginTransactionAsync()
                let readerWriterType = DBLoggerType.Writer ||| DBLoggerType.Reader
                do! queryCriteria.SetQueryRangeAsync(queryCriteria.ModelId, conn, tr)

                let! logSet = createLogInfoSetCommonAsync(queryCriteria, commonAppSettings, systems, conn, tr, readerWriterType)
                assert(logSet.QuerySet.ModelId = queryCriteria.ModelId)
                if queryCriteria <> null then
                    let! existingLogs =
                        conn.QueryAsync<ORMLog>(
                            $"SELECT * FROM [{Tn.Log}] WHERE modelId = @ModelId AND at BETWEEN @START AND @END ORDER BY id;",
                            {| START = queryCriteria.StartTime
                               END = queryCriteria.EndTime
                               ModelId = queryCriteria.ModelId|}
                        )
                    logSet.InitializeForReader(existingLogs)

                do! tr.CommitAsync()

                return logSet
            }

        member x.EnqueLogs (ys: DsLog seq) : unit =
            let toDecimal (value: obj) =
                match toBool value with
                | Bool b -> (if b then 1 else 0) |> decimal
                | UInt64 d -> decimal d
                | _ -> failwithlog "ERROR"


            verify (x.LogSet.Value.ReaderWriterType.HasFlag(DBLoggerType.Writer))

            for y in ys do
                match y.Storage.Target with
                | Some t ->
                    let key = y.Storage.TagKind, t.QualifiedName
                    let storageId = x.LogSet.Value.Storages[key].Id

                    if storageId = 0 then
                        noop ()

                    let value = toDecimal y.Storage.BoxedValue
                    let modelId = x.LogSet.Value.QuerySet.ModelId
                    ORMLog(-1, storageId, y.Time, value, modelId, y.TokenId) |> queue.Enqueue
                | None -> failwithlog "NOT yet!!"

        member x.EnqueLog (log: DsLog) : unit = x.EnqueLogs ([ log ])

        member x.InsertValueLog(time: DateTime, tag: TagEvent, tokenId:TokenIdType) =
            let vlog = DBLog.ValueLog(time, tag, tokenId)
            if tag.IsNeedSaveDBLog() then
                x.EnqueLog(vlog)
            vlog

        override x.Dispose() =
            base.Dispose()

        /// Log DB schema 생성: model 정보 없이, database schema 만 생성
        static member CreateSchemaAsync (commonAppSettings: DSCommonAppSettings) (cleanExistingDb:bool) =
            task {
                logDebug $":::initializeLogDbOnDemandAsync()"
                let loggerDBSettings = commonAppSettings.LoggerDBSettings
                if cleanExistingDb then
                    loggerDBSettings.DropDatabase()
                loggerDBSettings.FillModelId() |> ignore
            }


        static member CreateAsync
            (
                queryCriteria: QueryCriteria,      // reader + writer 인 경우에만 non null 값
                systems: DsSystem seq,
                cleanExistingDb: bool
            ) =
            task {
                let commonAppSettings = queryCriteria.CommonAppSettings
                do! DbWriter.CreateSchemaAsync commonAppSettings cleanExistingDb
                let dbWriter = new DbWriter(queryCriteria)
                let! logSet = dbWriter.createLogSetForWriterAsync queryCriteria systems
                dbWriter.LogSet <- Some logSet

                commonAppSettings.LoggerDBSettings.SyncInterval
                    .Subscribe(fun counter -> dbWriter.writePeriodicAsync(counter).Wait())
                    |> dbWriter.Disposables.Add

                let theSystem = systems.First()
                let rt, mt, st = int VertexTag.realToken, int VertexTag.mergeToken, int VertexTag.sourceToken
                TagEventSubject.Subscribe(fun tag ->
                    let _ =
                        option {
                            let now = DateTime.Now
                            let mutable tokenId = TokenIdType()
                            if tag.GetSystem() = theSystem && tag.IsVertexTokenTag() then
                                let target = tag.GetTarget()
                                let t = tag.TagKind
                                if t = rt then
                                    let real = target :?> Real
                                    let! realToken = real.GetRealToken()
                                    tokenId <- TokenIdType(dbWriter.GetTokenId(realToken));
                                elif t = mt then
                                    let real = target :?> Real
                                    let! branchToken = real.GetRealToken()  //삭제된 자신 토큰번호
                                    let! trunkToken  = real.GetMergeToken() //삭제한 메인경로 토큰번호
                                    dbWriter.OnTokenMerged(branchToken, trunkToken) |> ignore
                                elif t = st then
                                    let call = target :?> Call
                                    let sourceToken = call.GetSourceToken()
                                    dbWriter.AllocateTokenId(sourceToken, now) |> ignore

                            dbWriter.InsertValueLog(now, tag, tokenId) |> ignore
                        }
                    ()

                ) |> dbWriter.Disposables.Add

                DbWriter.TheDbWriter <- dbWriter
                return dbWriter
            }


