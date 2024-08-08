namespace Engine.Info

open System
open Dapper
open Engine.Core
open Dual.Common.Core.FS
open System.Threading.Tasks
open DBLoggerORM


[<AutoOpen>]
module DBReaderModule =
    /// DB log reader.  Dashboard 및 CCTV 등
    type DbReader() =
        /// 주기적으로 DB -> memory 로 log 를 read
        static let readPeriodicAsync (nPeriod: int64, queryCriteria: QueryCriteria) =
            task {
                use conn = queryCriteria.CommonAppSettings.CreateConnection()

                if nPeriod % 10L = 0L then
                    let! dbDsConfigJsonPath = queryPropertyAsync (queryCriteria.ModelId, PropName.ConfigPath, conn, null)

                    if dbDsConfigJsonPath <> queryCriteria.DsConfigJsonPath then
                        failwithlogf
                            $"DS Source file change detected:\r\n\t{dbDsConfigJsonPath} <> {queryCriteria.DsConfigJsonPath}"

                let lastLogId =
                    match logSet.TheLastLog with
                    | Some l -> l.Id
                    | _ -> -1

                let! newLogs =
                    conn.QueryAsync<ORMLog>(
                        $"""SELECT * FROM [{Tn.Log}]
                            WHERE modelId = @ModelId AND id > @LastLogId ORDER BY id DESC;""",
                        {| ModelId = queryCriteria.ModelId; LastLogId = lastLogId; |}
                    )
                // TODO: logSet.QuerySet.StartTime, logSet.QuerySet.EndTime 구간 내의 것만 필터
                if newLogs.any () then
                    let newLogs = newLogs |> map (ormLog2Log logSet) |> toList
                    logDebug $"Feteched {newLogs.length ()} new logs."
                    logSet.BuildIncremental newLogs
            }


        static let createLoggerInfoSetForReaderAsync (queryCriteria: QueryCriteria, commonAppSettings: DSCommonAppSettings, systems: DsSystem seq) : Task<LogSet> =
            task {
                use conn = queryCriteria.CommonAppSettings.CreateConnection()
                use! tr = conn.BeginTransactionAsync()

                let modelId = queryCriteria.ModelId
                let! dsConfigJson = queryPropertyAsync (modelId, PropName.ConfigPath, conn, tr)
                queryCriteria.DsConfigJsonPath <- dsConfigJson

                do! queryCriteria.SetQueryRangeAsync(modelId, conn, tr)
                let! logSet = createLogInfoSetCommonAsync (queryCriteria, commonAppSettings, systems, conn, tr, DBLoggerType.Reader)

                let! existingLogs =
                    conn.QueryAsync<ORMLog>(
                        $"SELECT * FROM [{Tn.Log}] WHERE modelId = @ModelId AND  at BETWEEN @START AND @END ORDER BY id;",
                        {| START = queryCriteria.StartTime
                           END = queryCriteria.EndTime
                           ModelId = modelId|}
                    )

                do! checkDbForReaderAsync (conn, tr)

                do! tr.CommitAsync()

                logSet.InitializeForReader(existingLogs)
                return logSet
            }




        static member initializeLogReaderOnDemandAsync (queryCriteria: QueryCriteria, systems: DsSystem seq) =

            task {
                let loggerDBSettings = queryCriteria.CommonAppSettings.LoggerDBSettings
                let! logSet_ = createLoggerInfoSetForReaderAsync (queryCriteria, queryCriteria.CommonAppSettings, systems)
                logSet <- logSet_

                loggerDBSettings.SyncInterval.Subscribe(fun counter -> readPeriodicAsync(counter, queryCriteria).Wait())
                |> logSet_.Disposables.Add

                return logSet_
            }

        static member changeQueryDurationAsync (logSet: LogSet, queryCriteria: QueryCriteria) =
            DbReader.initializeLogReaderOnDemandAsync (queryCriteria, logSet.Systems)


