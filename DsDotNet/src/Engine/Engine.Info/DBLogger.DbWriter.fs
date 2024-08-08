namespace Engine.Info
open System
open Dapper
open Engine.Core
open Dual.Common.Core.FS
open Dual.Common.Db
open System.Threading.Tasks
open System.Collections.Concurrent
open DBLoggerORM

[<AutoOpen>]
module DBWriterModule =
    [<AbstractClass>]
    type DbHandler(commonAppSettings: DSCommonAppSettings, logSet:LogSet option) =
        member val LogSet = logSet with get, set
        member x.CommonAppSettings = commonAppSettings

    /// DB log writer.  Runtime engine
    type DbWriter(commonAppSettings: DSCommonAppSettings, logSet) =
        inherit DbHandler(commonAppSettings, logSet)
        let queue = ConcurrentQueue<ORMLog>()

        new(commonAppSettings) = DbWriter(commonAppSettings, None)

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
                    use conn = commonAppSettings.CreateConnection()
                    use! tr = conn.BeginTransactionAsync()

                    if (x.LogSet.Value.ReaderWriterType.HasFlag(DBLoggerType.Reader)) then
                        let newLogs = newLogs |> map (ormLog2Log x.LogSet.Value) |> toList
                        x.LogSet.Value.BuildIncremental newLogs
                    let currentModelId = commonAppSettings.LoggerDBSettings.ModelId
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
                                (at, storageId, value, modelId, token)
                                VALUES (@At, @StorageId, @Value, @ModelId, @Token)
                            """

                        let! _ =
                            conn.ExecuteAsync(
                                query,
                                {| At = l.At
                                   StorageId = l.StorageId
                                   Value = l.Value
                                   ModelId = modelId
                                   Token = l.Token |},
                                tr
                            )
                        ()

                    do! tr.CommitAsync()
            }

        member x.writePeriodicAsync = x.dequeAndWriteDBAsync


        member x.createLogInfoSetForWriterAsync (queryCriteria: QueryCriteria) (systems: DsSystem seq) : Task<LogSet> =
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



        /// Log DB schema 생성
        static member InitializeLogDbOnDemandAsync (commonAppSettings: DSCommonAppSettings) (cleanExistingDb:bool) =
            task {
                logDebug $":::initializeLogDbOnDemandAsync()"
                let loggerDBSettings = commonAppSettings.LoggerDBSettings
                if cleanExistingDb then
                    loggerDBSettings.DropDatabase()
                loggerDBSettings.FillModelId() |> ignore
                return DbWriter(commonAppSettings)
            }


        static member InitializeLogWriterOnDemandAsync
            (
                queryCriteria: QueryCriteria,      // reader + writer 인 경우에만 non null 값
                systems: DsSystem seq,
                cleanExistingDb: bool
            ) =
            task {
                let commonAppSettings = queryCriteria.CommonAppSettings
                let! dbWriter = DbWriter.InitializeLogDbOnDemandAsync commonAppSettings cleanExistingDb
                let! logSet = dbWriter.createLogInfoSetForWriterAsync queryCriteria systems
                dbWriter.LogSet <- Some logSet

                commonAppSettings.LoggerDBSettings.SyncInterval.Subscribe(fun counter -> dbWriter.writePeriodicAsync(counter).Wait())
                |> logSet.Disposables.Add

                return dbWriter
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
                    ORMLog(-1, storageId, y.Time, value, modelId, y.Token) |> queue.Enqueue
                | None -> failwithlog "NOT yet!!"

        member x.EnqueLog (log: DsLog) : unit = x.EnqueLogs ([ log ])

