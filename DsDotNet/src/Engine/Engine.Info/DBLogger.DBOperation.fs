namespace Engine.Info

open Dapper
open Engine.Core
open Dual.Common.Core.FS
open Dual.Common.Db
open System
open System.Data
open System.Linq
open System.Threading.Tasks
open DBLoggerORM


[<AutoOpen>]
module internal DBLoggerImpl =
    /// for debugging purpose only!
    let mutable ORMDBSkeleton4Debug = getNull<ORMDBSkeleton>()

    let checkDbForReaderAsync (conn: IDbConnection, tr: IDbTransaction) =
        task {
            let! newTagKindInfos = getNewTagKindInfosAsync (conn, tr)

            if newTagKindInfos.any () then
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
        task {
            let modelId = queryCriteria.CommonAppSettings.LoggerDBSettings.ModelId
            let connStr = commonAppSettings.ConnectionString    // only for error message

            let systemStorages =
                systems
                |> collect(fun s -> s.GetStorages(true))
                |> distinct
                |> toArray

            if readerWriterType = DBLoggerType.Reader && not <| conn.IsTableExistsAsync(Tn.Storage).Result then
                failwithlogf $"Database not ready for {connStr}"

            let! dbStorages = conn.QueryAsync<ORMStorage>($"SELECT * FROM [{Tn.Storage}] WHERE modelId = {modelId}")
            let! dbMaintenances = conn.QueryAsync<ORMMaintenance>($"SELECT * FROM [{Tn.Maintenance}] WHERE modelId = {modelId}")

            let dbStorageDic      = dbStorages |> map (fun s -> getStorageKey s, s) |> Tuple.toDictionary
            let dbMaintenancesDic = dbMaintenances.ToDictionary(_.StorageId, id)

            let ormStorages: ORMStorage[] =
                systemStorages

                //|> map (fun s ->
                //    // todo:
                //    // IStorage level 에서 min/max duration 설정 치를 파악할 수 있어야 한다.
                //    //
                //    // s 로부터 min/max duration 값을 구하고, DB maintenance table 에 이미 값이 존재하면, 그것의 id 를 사용하고,
                //    // 없으면 새로운 row 를 삽입하고 그 id 를 maintenance id 로 할당...
                //    conn.QueryFirstOrDefault(
                //        $"""SELECT * FROM {Tn.Maintenance}
                //            WHERE modelId = {modelId} AND storageId = {}""")
                //    let minDuration = nullDuration
                //    let maxDuration = nullDuration
                //    let maintenanceId = nullId
                //    ORMStorage(s, maintenanceId))
                |> map ORMStorage

            let existingStorages, newStorages =
                ormStorages
                |> Array.partition (fun s -> dbStorageDic.ContainsKey(getStorageKey s))

            // 메모리 상의 ORMStorage 에 대해서 DB 에서 읽어온 id mapping
            for s in existingStorages do
                s.Id <- dbStorageDic[getStorageKey s].Id
                match dbMaintenancesDic.TryGetValue(s.Id) with
                | true, maintenace -> s.MaintenanceId <- maintenace.Id
                | _ -> ()

            if newStorages.any () && readerWriterType = DBLoggerType.Reader then
                failwithlogf $"Database can't be sync'ed for {connStr}"

            for s in newStorages do
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

            return new LogSet(queryCriteria, systems, existingStorages @ newStorages, readerWriterType)
        }





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
