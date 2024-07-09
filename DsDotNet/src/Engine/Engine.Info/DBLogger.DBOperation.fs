namespace Engine.Info

open System
open Dapper
open Engine.Core
open Microsoft.Data.Sqlite
open Dual.Common.Core.FS
open Dual.Common.Db
open System.Data
open System.Threading.Tasks
open System.Collections.Generic
open System.Collections.Concurrent
open DBLoggerORM


[<AutoOpen>]
module internal DBLoggerImpl =
    type private Observable = System.Reactive.Linq.Observable

    let createConnectionWith (connStr) =
        new SqliteConnection(connStr) |> tee (fun conn -> conn.Open())

    let getNewTagKindInfosAsync (conn: IDbConnection, tr: IDbTransaction) =
        let tagKindInfos = GetAllTagKinds ()

        task {
            let! existingTagKindMap = conn.QueryAsync<ORMTagKind>($"SELECT * FROM [{Tn.TagKind}];", null, tr)

            let existingTagKindHash =
                existingTagKindMap |> map (fun t -> t.Id, t.Name) |> HashSet

            return tagKindInfos |> filter (fun t -> not <| existingTagKindHash.Contains(t))
        }

    let checkDbForReaderAsync (conn: IDbConnection, tr: IDbTransaction) =
        task {
            let! newTagKindInfos = getNewTagKindInfosAsync (conn, tr)

            if newTagKindInfos.any () then
                failwithlogf $"Database sync failed."
        }




    let ormLog2Log (logSet: LogSet) (l: ORMLog) =
        let storage = logSet.StoragesById[l.StorageId]
        Log(l.Id, storage, l.At, l.Value)

    type LogSet with
        // ormLogs: id 순(시간 순) 정렬
        member x.InitializeForReader(ormLogs: ORMLog seq) =
            x.StoragesById.Clear()

            for s in x.Storages.Values do
                x.StoragesById.Add(s.Id, s)

            let logs = ormLogs |> map (ormLog2Log x) |> toArray

            let groups =
                logs |> Seq.groupBy (fun l -> getStorageKey x.StoragesById[l.StorageId])

            for (key, group) in groups do
                // 사용자가 시작 기간을 지정한 경우에는, 최초 log 를 ON 이 되도록 정렬
                let logsWithStartON =
                    let userSpecifiedStart = x.QuerySet.TargetStart.IsSome

                    if userSpecifiedStart then
                        group |> Seq.skipWhile isOff
                    else
                        group

                x.Summaries[key].Build(logsWithStartON)

            if logs.any () then
                x.LastLog <- logs |> Seq.tryLast

    let mutable logSet = getNull<LogSet> ()

    let private createLogInfoSetCommonAsync
        (
            querySet: QuerySet,
            commonAppSetting: DSCommonAppSettings,
            systems: DsSystem seq,
            conn: IDbConnection,
            tr: IDbTransaction,
            readerWriterType: DBLoggerType
        ) : Task<LogSet> =
        task {
            let systemStorages: Storage array =
                systems
                |> map (fun s -> s.TagManager)
                |> distinct
                |> Seq.collect (fun tagManager -> tagManager.Storages.Values)
                |> filter (fun s -> s.TagKind <> skipValueChangedForTagKind) // 내부변수
                |> distinct
                |> map Storage
                |> toArray

            let connStr = commonAppSetting.LoggerDBSettings.ConnectionString
            if readerWriterType = DBLoggerType.Reader && not <| conn.IsTableExistsAsync(Tn.Storage).Result then
                failwithlogf $"Database not ready for {connStr}"

            let! dbStorages = conn.QueryAsync<Storage>($"SELECT * FROM [{Tn.Storage}]")

            let dbStorages =
                dbStorages |> map (fun s -> getStorageKey s, s) |> Tuple.toDictionary


            let existingStorages, newStorages =
                systemStorages
                |> Seq.partition (fun s -> dbStorages.ContainsKey(getStorageKey s))

            for s in existingStorages do
                s.Id <- dbStorages[getStorageKey s].Id

            if newStorages.any () then
                if readerWriterType = DBLoggerType.Reader then
                    failwithlogf $"Database can't be sync'ed for {connStr}"
                else
                    let modelId = querySet.CommonAppSettings.LoggerDBSettings.ModelId
                    for s in newStorages do
                        let! id =
                            conn.InsertAndQueryLastRowIdAsync(
                                tr,
                                $"""INSERT INTO [{Tn.Storage}]
                                (name, fqdn, tagKind, dataType, modelId)
                                VALUES (@Name, @Fqdn, @TagKind, @DataType, @ModelId)
                                ;
                            """,
                                {| Name = s.Name
                                   Fqdn = s.Fqdn
                                   TagKind = s.TagKind
                                   DataType = s.DataType
                                   ModelId = modelId |}
                            )

                        s.Id <- id

            return new LogSet(querySet, systems, existingStorages @ newStorages, readerWriterType)
        }

    /// DB log writer.  Runtime engine
    [<AutoOpen>]
    module Writer =
        let queue = ConcurrentQueue<ORMLog>()

        /// 주기적으로 memory -> DB 로 log 를 write
        let dequeAndWriteDBAsync (nPeriod: int64, commonAppSetting: DSCommonAppSettings) =
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
                    let connStr = commonAppSetting.LoggerDBSettings.ConnectionString
                    logDebug $"{DateTime.Now}: Writing {newLogs.length ()} new logs."
                    use conn = createConnectionWith connStr
                    use! tr = conn.BeginTransactionAsync()

                    if (logSet.ReaderWriterType.HasFlag(DBLoggerType.Reader)) then
                        let newLogs = newLogs |> map (ormLog2Log logSet) |> toList
                        logSet.BuildIncremental newLogs
                    let modelId = commonAppSetting.LoggerDBSettings.ModelId
                    for l in newLogs do
                        let query =
                            $"""INSERT INTO [{Tn.Log}]
                                (at, storageId, value, modelId)
                                VALUES (@At, @StorageId, @Value, @ModelId)
                            """

                        do!
                            conn.ExecuteSilentlyAsync(
                                query,
                                {| At = l.At
                                   StorageId = l.StorageId
                                   Value = l.Value
                                   ModelId = modelId |}
                            )

                        ()

                    do! tr.CommitAsync()
            }

        let writePeriodicAsync = dequeAndWriteDBAsync

        let enqueLogsForInsert (xs: DsLog seq) =
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
                    ORMLog(-1, storageId, x.Time, value) |> queue.Enqueue
                | None -> failwithlog "NOT yet!!"

        let enqueLogForInsert (x: DsLog) = enqueLogsForInsert ([ x ])


        let createLogInfoSetForWriterAsync (querySet: QuerySet) (commonAppSetting: DSCommonAppSettings) (systems: DsSystem seq) : Task<LogSet> =
            task {
                let connStr = commonAppSetting.LoggerDBSettings.ConnectionString
                use conn = createConnectionWith connStr
                use! tr = conn.BeginTransactionAsync()
                let mutable readerWriterType = DBLoggerType.Writer
                if querySet <> null then
                    readerWriterType <- readerWriterType ||| DBLoggerType.Reader
                    do! querySet.SetQueryRangeAsync(querySet.ModelId, conn, tr)

                let! logSet = createLogInfoSetCommonAsync(querySet, commonAppSetting, systems, conn, tr, readerWriterType)
                if querySet <> null then
                    let! existingLogs =
                        conn.QueryAsync<ORMLog>(
                            $"SELECT * FROM [{Tn.Log}] WHERE at BETWEEN @START AND @END ORDER BY id;",
                            {| START = querySet.StartTime
                               END = querySet.EndTime |}
                        )
                    logSet.InitializeForReader(existingLogs)

                do! tr.CommitAsync()

                return logSet
            }

        let createLoggerDBSchemaAsync (connStr:string) (modelZipPath:string) (dbWriter:string) (cleanExistingDb:bool) =
            task {
                use conn = createConnectionWith connStr

                if cleanExistingDb then
                    conn.DropDatabase();

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
                    let query = $"INSERT INTO [{Tn.TagKind}] (id, name, modelId) VALUES (@Id, @Name, @ModelId);"
                    do! conn.ExecuteSilentlyAsync(query, {| Id = id; Name = name; ModelId=modelId |}, tr)

                do! tr.CommitAsync()
                return modelId
            }


        let fillLoggerDBSchemaAsync (connStr:string) (modelCompileInfo: ModelCompileInfo) =
            task {
                let pptPath, config = modelCompileInfo.PptPath, modelCompileInfo.ConfigPath

                use conn = createConnectionWith connStr
                let modelId = 1     // todo: modelId 추후 수정 필요

                do!
                    conn.ExecuteSilentlyAsync(
                        $"""INSERT OR REPLACE INTO [{Tn.Property}]
                                          (name, value, modelId)
                                          VALUES(@Name, @Value, @ModelId);""",
                        {| Name = PropName.PptPath
                           Value = pptPath
                           ModelId = modelId |} )

                do!
                    conn.ExecuteSilentlyAsync(
                        $"""INSERT OR REPLACE INTO [{Tn.Property}]
                                          (name, value, modelId)
                                          VALUES(@Name, @Value, @ModelId);""",
                        {| Name = PropName.ConfigPath
                           Value = config
                           ModelId = modelId |} )
            }

        /// Log DB schema 생성
        let initializeLogDbOnDemandAsync (commonAppSetting: DSCommonAppSettings) (cleanExistingDb:bool) =
            task {
                let loggerDBSettings = commonAppSetting.LoggerDBSettings
                let connStr = loggerDBSettings.ConnectionString
                let (modelZipPath, dbWriter:string) = loggerDBSettings.ModelFilePath, loggerDBSettings.DbWriter
                let! modelId = createLoggerDBSchemaAsync connStr modelZipPath dbWriter cleanExistingDb
                commonAppSetting.LoggerDBSettings.ModelId <- modelId
            }


        let initializeLogWriterOnDemandAsync
            (
                querySet: QuerySet,      // reader + writer 인 경우에만 non null 값
                commonAppSetting: DSCommonAppSettings,
                systems: DsSystem seq,
                modelCompileInfo: ModelCompileInfo,
                cleanExistingDb: bool
            ) =
            task {
                let connStr = commonAppSetting.LoggerDBSettings.ConnectionString
                do! initializeLogDbOnDemandAsync commonAppSetting cleanExistingDb
                do! fillLoggerDBSchemaAsync connStr modelCompileInfo
                let! logSet_ = createLogInfoSetForWriterAsync querySet commonAppSetting systems
                logSet <- logSet_

                commonAppSetting.LoggerDBSettings.SyncInterval.Subscribe(fun counter -> writePeriodicAsync(counter, commonAppSetting).Wait())
                |> logSet_.Disposables.Add

                return logSet_
            }


    /// DB log reader.  Dashboard 및 CCTV 등
    [<AutoOpen>]
    module Reader =
        /// 주기적으로 DB -> memory 로 log 를 read
        let readPeriodicAsync (nPeriod: int64, querySet: QuerySet) =
            task {
                let connStr = querySet.CommonAppSettings.LoggerDBSettings.ConnectionString
                use conn = createConnectionWith connStr

                if nPeriod % 10L = 0L then
                    let! dbDsConfigJsonPath = queryPropertyAsync (querySet.ModelId, PropName.ConfigPath, conn, null)

                    if dbDsConfigJsonPath <> querySet.DsConfigJsonPath then
                        failwithlogf
                            $"DS Source file change detected:\r\n\t{dbDsConfigJsonPath} <> {querySet.DsConfigJsonPath}"

                let lastLogId =
                    match logSet.LastLog with
                    | Some l -> l.Id
                    | _ -> -1

                let! newLogs =
                    conn.QueryAsync<ORMLog>(
                        $"""SELECT * FROM [{Tn.Log}] 
                            WHERE id > {lastLogId} ORDER BY id DESC;"""
                    )
                // TODO: logSet.QuerySet.StartTime, logSet.QuerySet.EndTime 구간 내의 것만 필터
                if newLogs.any () then
                    let newLogs = newLogs |> map (ormLog2Log logSet) |> toList
                    logDebug $"Feteched {newLogs.length ()} new logs."
                    logSet.BuildIncremental newLogs
            }


        let createLoggerInfoSetForReaderAsync (querySet: QuerySet, commonAppSetting: DSCommonAppSettings, systems: DsSystem seq) : Task<LogSet> =
            task {
                let connStr = querySet.CommonAppSettings.LoggerDBSettings.ConnectionString
                use conn = createConnectionWith connStr
                use! tr = conn.BeginTransactionAsync()

                let modelId = querySet.ModelId
                let! dsConfigJson = queryPropertyAsync (modelId, PropName.ConfigPath, conn, tr)
                querySet.DsConfigJsonPath <- dsConfigJson

                do! querySet.SetQueryRangeAsync(modelId, conn, tr)
                let! logSet = createLogInfoSetCommonAsync (querySet, commonAppSetting, systems, conn, tr, DBLoggerType.Reader)

                let! existingLogs =
                    conn.QueryAsync<ORMLog>(
                        $"SELECT * FROM [{Tn.Log}] WHERE at BETWEEN @START AND @END ORDER BY id;",
                        {| START = querySet.StartTime
                           END = querySet.EndTime |}
                    )

                do! checkDbForReaderAsync (conn, tr)

                do! tr.CommitAsync()

                logSet.InitializeForReader(existingLogs)
                return logSet
            }




        let initializeLogReaderOnDemandAsync (querySet: QuerySet, systems: DsSystem seq) =

            task {
                let loggerDBSettings = querySet.CommonAppSettings.LoggerDBSettings
                let! logSet_ = createLoggerInfoSetForReaderAsync (querySet, querySet.CommonAppSettings, systems)
                logSet <- logSet_

                loggerDBSettings.SyncInterval.Subscribe(fun counter -> readPeriodicAsync(counter, querySet).Wait())
                |> logSet_.Disposables.Add

                return logSet_
            }

        let changeQueryDurationAsync (logSet: LogSet, querySet: QuerySet) =
            initializeLogReaderOnDemandAsync (querySet, logSet.Systems)





    let count (logSet: LogSet, fqdns: string seq, tagKinds: int seq, value: bool) =
        let mutable count = 0

        for fqdn in fqdns do
            for tagKind in tagKinds do
                let summary = logSet.GetSummary(tagKind, fqdn)
                count <- count + summary.Count

        count

    /// 지정된 조건에 따라 마지막 'Value'를 반환
    let getLastValue (logSet: LogSet, fqdn: string, tagKind: int) : bool option =
        option {
            let! lastLog = logSet.GetSummary(tagKind, fqdn).LastLog
            return lastLog.Value |> toBool
        }

    /// LogSet 설정 이전에, connection string 기준으로 database 조회해서 현재 db 에 저장된
    /// DS json config file 의 경로를 반환
    let queryPropertyDsConfigJsonPathWithConnectionStringAsync (connectionString: string) =
        use conn = createConnectionWith (connectionString)
        failwith "Not yet implemented"
        let modelId = -1
        queryPropertyAsync (modelId, PropName.ConfigPath, conn, null)
