namespace Engine.Info

open Engine.Core
open Dual.Common.Core.FS
open System
open System.IO




type DBLogger() =
    //static let queryCriteria = QuerySet()
    //static let queryCriteria = QuerySet(Nullable<DateTime>(DateTime(2023, 10, 28, 10, 46, 0)), Nullable<DateTime>())

    static member EnqueLog(log: DsLog) =
        DbWriter.enqueLog (log: DsLog)

    static member EnqueLogs(logs: DsLog seq) =
        DbWriter.enqueLogs (logs: DsLog seq)

    static member InitializeLogWriterOnDemandAsync
        (
            queryCriteria: QueryCriteria,
            systems: DsSystem seq,
            cleanExistingDb: bool
        ) =
        ()
        task {
            Log4NetWrapper.logWithTrace <- true

            let! logSet =
                DbWriter.initializeLogWriterOnDemandAsync (queryCriteria, systems, cleanExistingDb)

            return logSet :> ILogSet
        }

    /// model 정보 없이, database schema 만 생성
    static member InitializeLogDbOnDemandAsync (commonAppSettings: DSCommonAppSettings, cleanExistingDb:bool) =
        DbWriter.initializeLogDbOnDemandAsync commonAppSettings cleanExistingDb



    static member InitializeLogReaderOnDemandAsync(queryCriteria: QueryCriteria, systems: DsSystem seq) =
        task {
            Log4NetWrapper.logWithTrace <- true
            let! logSet = DbReader.initializeLogReaderOnDemandAsync (queryCriteria, systems)
            return logSet :> ILogSet
        }

    /// Reader + Writer 일체형
    static member InitializeLogReaderWriterOnDemandAsync
        (
            queryCriteria: QueryCriteria,
            systems: DsSystem seq,
            cleanExistingDb:bool
        ) =
        ()
        task {
            Log4NetWrapper.logWithTrace <- true

            let! logSet =
                DbWriter.initializeLogWriterOnDemandAsync (queryCriteria, systems, cleanExistingDb)

            return logSet //:> ILogSet
        }



    /// 조회 기간 변경 (reader)
    /// call site 에서는 기존 인자로 주어진 logSet 은 자동 dispose 되며, 새로 return 되는 logSet 을 이용하여야 한다.
    [<Obsolete("Not yet implemented")>]
    static member ChangeQueryDurationAsync(commonAppSettings:DSCommonAppSettings, logSet: ILogSet, startAt: Nullable<DateTime>, endAt: Nullable<DateTime>) =
        task {
            let logSet = logSet :?> LogSet
            failwith "Not yet implemented"
            let modelId = -1
            let queryCriteria = QueryCriteria(commonAppSettings, modelId, startAt, endAt)
            let! newLogSet = DbReader.changeQueryDurationAsync (logSet, queryCriteria)
            dispose (logSet :> IDisposable)
            return newLogSet :> ILogSet
        }

    // { unit test 등의 debugging 용
    static member internal Count(fqdns: string seq, tagKinds: int seq, logSet: LogSet) =
        DBLoggerImpl.count (logSet, fqdns, tagKinds, true)

    static member internal Count(fqdn: string, tagKind: int, logSet: LogSet) =
        DBLogger.Count([| fqdn |], [| tagKind |], logSet)

    static member internal GetLastValue(fqdn: string, tagKind: int, logSet: LogSet) =
        DBLoggerImpl.getLastValue(logSet, fqdn, tagKind).Value

    static member internal Sum(fqdn, tagKind, logSet: LogSet) =
        DBLoggerQueryImpl.sum (logSet, fqdn, tagKind)

    static member internal Average(fqdn, tagKind, logSet: LogSet) =
        DBLoggerQueryImpl.average (logSet, fqdn, tagKind)
    // }

    static member Count(fqdns: string seq, tagKinds: int seq) =
        DBLoggerImpl.count (DBLoggerImpl.logSet, fqdns, tagKinds, true)

    static member Count(fqdn: string, tagKind: int) =
        DBLogger.Count([| fqdn |], [| tagKind |])

    static member GetLastValue(fqdn: string, tagKind: int) =
        DBLoggerImpl.getLastValue (DBLoggerImpl.logSet, fqdn, tagKind)
        |> Option.toNullable


    static member Sum(fqdn, tagKind) =
        DBLoggerQueryImpl.sum (DBLoggerImpl.logSet, fqdn, tagKind)

    static member Average(fqdn, tagKind) =
        DBLoggerQueryImpl.average (DBLoggerImpl.logSet, fqdn, tagKind)

    static member GetDsFilePath(connectionString: string) =
        let filePathOption = connectionString.Split('=').TryLast()
        match filePathOption with
        | Some filePath ->
            if not <| File.Exists filePath then
                failwithf $"{filePath} does not exist."
        | None ->
            failwithf $"{connectionString} is in the wrong format. Expected format: Data Source=Path..."

        DBLoggerImpl
            .queryPropertyDsConfigJsonPathWithConnectionStringAsync(connectionString)
            .Result
