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
        Log(l.Id, storage, l.At, l.Value, l.ModelId, l.Token)

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

                x.Summaries[key].Build(logsWithStartON, x.LastLogs)

            x.TheLastLog <- logs |> Seq.tryLast

    let mutable logSet = getNull<LogSet> ()

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
            let systemStorages: ORMStorage array =
                systems
                |> collect(fun s -> s.GetStorages(true))
                |> distinct
                |> map ORMStorage
                |> toArray

            let modelId = queryCriteria.CommonAppSettings.LoggerDBSettings.ModelId
            let connStr = commonAppSettings.ConnectionString
            if readerWriterType = DBLoggerType.Reader && not <| conn.IsTableExistsAsync(Tn.Storage).Result then
                failwithlogf $"Database not ready for {connStr}"

            let! dbStorages = conn.QueryAsync<ORMStorage>($"SELECT * FROM [{Tn.Storage}] WHERE modelId = {modelId}")

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
