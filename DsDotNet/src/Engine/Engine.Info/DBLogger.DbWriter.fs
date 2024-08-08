namespace Engine.Info
open System
open Dapper
open Engine.Core
open Dual.Common.Core.FS
open Dual.Common.Db
open System.Data
open System.Threading.Tasks
open System.Collections.Generic
open System.Collections.Concurrent
open DBLoggerORM

[<AutoOpen>]
module DBWriterModule =
    /// DB log writer.  Runtime engine
    type DbWriter() =
        static let queue = ConcurrentQueue<ORMLog>()

        /// 주기적으로 memory -> DB 로 log 를 write
        static let dequeAndWriteDBAsync (nPeriod: int64, commonAppSettings: DSCommonAppSettings) =
            verify (logSet.ReaderWriterType.HasFlag(DBLoggerType.Writer))

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

                    if (logSet.ReaderWriterType.HasFlag(DBLoggerType.Reader)) then
                        let newLogs = newLogs |> map (ormLog2Log logSet) |> toList
                        logSet.BuildIncremental newLogs
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

        static member writePeriodicAsync = dequeAndWriteDBAsync


        static member createLogInfoSetForWriterAsync (queryCriteria: QueryCriteria) (systems: DsSystem seq) : Task<LogSet> =
            task {
                let commonAppSettings = queryCriteria.CommonAppSettings

                let! dbSckeleton = ORMDBSkeletonDTOExt.CreateLoggerDBAsync(queryCriteria.ModelId, $"Data Source={commonAppSettings.LoggerDBSettings.ConnectionPath}")
                ORMDBSkeleton4Debug <- dbSckeleton

                use conn = commonAppSettings.CreateConnection()
                use! tr = conn.BeginTransactionAsync()
                let mutable readerWriterType = DBLoggerType.Writer
                readerWriterType <- readerWriterType ||| DBLoggerType.Reader
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

        static member createLoggerDBSchemaAsync (connStr:string) (modelZipPath:string) (dbWriter:string) =
            task {
                failwith "ERROR: LoggerDBSettings.FillModelId() ... 관련 호출로 일부 대체"
                use conn = createConnectionWith connStr
                use! tr = conn.BeginTransactionAsync()
                let! exists = conn.IsTableExistsAsync(Tn.Storage)

                if not exists then
                    // schema 새로 생성
                    do! conn.ExecuteSilentlyAsync(sqlCreateSchema, tr)
                    ()

                let lastModified = System.IO.FileInfo(modelZipPath).LastWriteTime
                let param:obj = {| Path=modelZipPath; LastModified=lastModified; Runtime=dbWriter |}
                let! models = conn.QueryAsync<int>($"SELECT id FROM [{Tn.Model}] WHERE path = @Path AND lastModified = @LastModified AND runtime=@Runtime;", param, tr)
                let modelId =
                    match models.ToFSharpList() with
                    | id::[] -> id
                    | [] ->
                        let id =
                            conn.InsertAndQueryLastRowIdAsync(tr,
                                $"""INSERT INTO [{Tn.Model}]
                                    (path, lastModified, runtime)
                                    VALUES (@Path, @LastModified, @Runtime)
                                """,
                                param
                            ).Result
                        id
                    | _ -> failwith "Multiple models found"

                let! newTagKindInfos = getNewTagKindInfosAsync (conn, tr)

                for (id, name) in newTagKindInfos do
                    let query = $"INSERT INTO [{Tn.TagKind}] (id, name) VALUES (@Id, @Name);"
                    do! conn.ExecuteSilentlyAsync(query, {| Id = id; Name = name |}, tr)

                do! tr.CommitAsync()
                return modelId
            }



        /// Log DB schema 생성
        static member initializeLogDbOnDemandAsync (commonAppSettings: DSCommonAppSettings) (cleanExistingDb:bool) =
            task {
                logDebug $":::initializeLogDbOnDemandAsync()"
                let loggerDBSettings = commonAppSettings.LoggerDBSettings
                if cleanExistingDb then
                    loggerDBSettings.DropDatabase()
                loggerDBSettings.FillModelId() |> ignore
            }


        static member initializeLogWriterOnDemandAsync
            (
                queryCriteria: QueryCriteria,      // reader + writer 인 경우에만 non null 값
                systems: DsSystem seq,
                cleanExistingDb: bool
            ) =
            task {
                let commonAppSettings = queryCriteria.CommonAppSettings
                do! DbWriter.initializeLogDbOnDemandAsync commonAppSettings cleanExistingDb
                let! logSet_ = DbWriter.createLogInfoSetForWriterAsync queryCriteria systems
                logSet <- logSet_

                commonAppSettings.LoggerDBSettings.SyncInterval.Subscribe(fun counter -> DbWriter.writePeriodicAsync(counter, commonAppSettings).Wait())
                |> logSet_.Disposables.Add

                return logSet_
            }


        static member enqueLogs (xs: DsLog seq) : unit =
            let toDecimal (value: obj) =
                match toBool value with
                | Bool b -> (if b then 1 else 0) |> decimal
                | UInt64 d -> decimal d
                | _ -> failwithlog "ERROR"


            verify (logSet.ReaderWriterType.HasFlag(DBLoggerType.Writer))

            for x in xs do
                match x.Storage.Target with
                | Some t ->
                    let key = x.Storage.TagKind, t.QualifiedName
                    let storageId = logSet.Storages[key].Id

                    if storageId = 0 then
                        noop ()

                    let value = toDecimal x.Storage.BoxedValue
                    let modelId = logSet.QuerySet.ModelId
                    ORMLog(-1, storageId, x.Time, value, modelId, x.Token) |> queue.Enqueue
                | None -> failwithlog "NOT yet!!"

        static member enqueLog (log: DsLog) : unit = DbWriter.enqueLogs ([ log ])

