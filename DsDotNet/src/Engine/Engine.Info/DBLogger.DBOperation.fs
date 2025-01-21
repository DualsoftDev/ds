namespace Engine.Info

open System.Data
open System.Linq
open System.Threading.Tasks
open Dapper
open Dapper.Contrib.Extensions

open Engine.Core
open Dual.Common.Core.FS
open Dual.Common.Db
open DBLoggerORM


[<AutoOpen>]
module internal DBLoggerImpl =
    /// for debugging purpose only!
    let mutable ORMDBSkeleton4Debug = getNull<ORMDBSkeleton>()

    let checkDbForReaderAsync (conn: IDbConnection, tr: IDbTransaction) =
        task {
            let! newTagKindInfos = getNewTagKindInfosAsync (conn, tr)

            if newTagKindInfos.Any () then
                failwithlogf $"Database sync failed."
        }

    let ormLog2Log (logSet: LogSet) (l: ORMLog) =
        let storage = logSet.StoragesById[l.StorageId]
        Log(l.Id, storage, l.At, l.Value, l.ModelId, l.TokenId)

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
                    match x.QuerySet.TargetStart with
                    | Some _userSpecifiedStart ->
                        group |> Seq.skipWhile isOff
                    | None ->
                        group

                x.Summaries[key].Build(logsWithStartON, x.LastLogs)

            x.TheLastLog <- logs |> Seq.tryLast


    let internal createLogInfoSetCommonAsync
        (
            queryCriteria: QueryCriteria,
            commonAppSettings: DSCommonAppSettings,
            systems: DsSystem seq,
            conn: IDbConnection,
            tr: IDbTransaction,
            readerWriterType: DBLoggerType
        ) : Task<LogSet> =

        let modelId = queryCriteria.CommonAppSettings.LoggerDBSettings.ModelId

        /// ormStorage 의 MaintenanceId 및 이에 해당하는 Maintenance row 를 수정 (add/delete/update)
        /// - System storage 의 Maintenance 정보가 신규추가 / 삭제 시, Maintenance Id 를 추가/삭제
        /// - System storage 의 Maintenance 정보가 변경시, Maintenance Id 에 해당하는 table 의 row 를 update
        let updateMaintenanceOfStorageAsync (storageRows:ORMStorage[])  =
            //let sysStorage = storageRow.Storage
            task {
                let! maintenanceRows = conn.QueryAsync<ORMMaintenance>($"SELECT * FROM [{Tn.Maintenance}] WHERE modelId = {modelId}")
                let dbMaintenancesDic = maintenanceRows.ToDictionary(_.StorageId, id)
                for r in storageRows do
                    let optMi, optId = r.Storage.MaintenanceInfo.Cast<MaintenanceInfo>(), r.MaintenanceId.ToOption()

                    match optMi, optId with
                    | Some mi, Some mid ->       // diff & update
                        let maintenanceRow = dbMaintenancesDic[r.Id]
                        assert (int64 maintenanceRow.Id = mid)
                        maintenanceRow.MinDuration <- mi.MinDuration
                        maintenanceRow.MaxDuration <- mi.MaxDuration
                        maintenanceRow.MaxNumOperation <- mi.MaxNumOperation
                        let! _ = conn.UpdateAsync(maintenanceRow, tr)
                        ()

                    | None, Some mid ->          // Maintenance row 삭제
                        r.MaintenanceId <- nullId
                        let! _ = conn.ExecuteAsync($"DELETE FROM [{Tn.Maintenance}] WHERE id = {mid}")
                        let! _ = conn.UpdateAsync(r, tr)
                        ()

                    | Some mi, None ->          // Maintenance row 추가하고 id 할당
                        let! maintenanceId =
                            conn.InsertAndQueryLastRowIdAsync(
                                tr,
                                $"""INSERT INTO [{Tn.Maintenance}]
                                    (minDuration, maxDuration, maxNumOperation, modelId, storageId)
                                    VALUES (@MinDuration, @MaxDuration, @MaxNumOperation, @ModelId, @StorageId)
                                    ;
                                """,
                                {|
                                    MinDuration = mi.MinDuration
                                    MaxDuration = mi.MaxDuration
                                    MaxNumOperation = mi.MaxNumOperation
                                    ModelId = modelId
                                    StorageId = r.Id
                                |}
                            )
                        r.MaintenanceId <- maintenanceId
                        let! _ = conn.UpdateAsync(r, tr)
                        ()

                    | None, None ->
                        noop()
            }


        let connStr = commonAppSettings.ConnectionString    // only for error message
        if readerWriterType = DBLoggerType.Reader && not <| conn.IsTableExistsAsync(Tn.Storage).Result then
            failwithlogf $"Database not ready for {connStr}"

        task {
            let systemStorages =
                systems
                |> collect(fun s -> s.GetStorages(true))
                |> distinct
                |> toArray


            let! storageRows = conn.QueryAsync<ORMStorage>($"SELECT * FROM [{Tn.Storage}] WHERE modelId = {modelId}")
            let dbStorageDic = storageRows |> map (fun s -> getStorageKey s, s) |> Tuple.toDictionary

            //Storage.Target 이 없으면 DB 관리 대상에서 제외
            let storageRows: ORMStorage[] = systemStorages |> filter(fun s->s.Target.IsSome) |> map(fun s -> ORMStorage(s, modelId))
            let existingStorageRows, newStorageRows =
                storageRows
                |> Array.partition (fun s -> dbStorageDic.ContainsKey(getStorageKey s))

            // 메모리 상의 ORMStorage 에 대해서 DB 에서 읽어온 id mapping
            for r in existingStorageRows do
                let dbRow = dbStorageDic[getStorageKey r]
                r.Id <- dbRow.Id
                r.MaintenanceId <- dbRow.MaintenanceId

            if newStorageRows.Any () && readerWriterType = DBLoggerType.Reader then
                failwithlogf $"Database can't be sync'ed for {connStr}"

            for s in newStorageRows do
                let! id =
                    conn.InsertAndQueryLastRowIdAsync(
                        tr,
                        $"""INSERT INTO [{Tn.Storage}]
                            (name, fqdn, tagKind, dataType, modelId)
                            VALUES (@Name, @Fqdn, @TagKind, @DataType, @ModelId)
                            ;
                        """,
                        {|
                            Name = s.Name
                            Fqdn = s.Fqdn
                            TagKind = s.TagKind
                            DataType = s.DataType
                            ModelId = modelId
                        |}
                    )

                s.Id <- id

#if DEBUG_HARDCORE
            (* 임의 조작 *)
            for r in storageRows.Where(fun r -> r.TagKind = int VertexTag.going) do   // 11007
                if r.Id % 2 = 0 then
                    r.Storage.MaintenanceInfo <- Some <| MaintenanceInfo(Nullable(1L), Nullable(3L), Nullable(1L))
            noop()
#endif
            do! updateMaintenanceOfStorageAsync storageRows

            return new LogSet(queryCriteria, systems, storageRows, readerWriterType)
        }


    let count (logSet: LogSet, fqdns: string seq, tagKinds: int seq, value: bool) =
        seq {
            for fqdn in fqdns do
                for tagKind in tagKinds do
                    let summary = logSet.GetSummary(tagKind, fqdn)
                    yield summary.Count
        } |> Seq.sum

    /// 지정된 조건에 따라 마지막 'Value'를 반환
    let getLastValue (logSet: LogSet, fqdn: string, tagKind: int) : bool option =
        option {
            let! storage = logSet.Storages.TryFindValue((tagKind, fqdn))
            let! lastLog = logSet.LastLogs.TryFindValue(storage)
            return lastLog.Value |> toBool
        }

    /// LogSet 설정 이전에, connection string 기준으로 database 조회해서 현재 db 에 저장된
    /// DS json config file 의 경로를 반환
    let queryPropertyDsConfigJsonPathWithConnectionStringAsync (connectionString: string) =
        use conn = createConnectionWith (connectionString)
        failwith "Not yet implemented"
        let modelId = -1
        queryPropertyAsync (modelId, PropName.ConfigPath, conn, null)
