namespace Engine.Info

open Engine.Core
open Dual.Common.Core.FS
open System


type DBLogger() =
    static let querySet = QuerySet()
    //static let querySet = QuerySet(Nullable<DateTime>(DateTime(2023, 10, 28, 10, 46, 0)), Nullable<DateTime>())

    static member EnqueLogForInsert(log:DsLog) = DBLoggerImpl.Writer.enqueLogForInsert(log:DsLog)
    static member EnqueLogsForInsert(logs:DsLog seq) = DBLoggerImpl.Writer.enqueLogsForInsert(logs:DsLog seq)
    static member InitializeLogWriterOnDemandAsync(systems:DsSystem seq, commonAppSetting:DSCommonAppSettings, modelCompileInfo:ModelCompileInfo) =
        task {
            Log4NetWrapper.logWithTrace <- true
            let! logSet = DBLoggerImpl.Writer.initializeLogWriterOnDemandAsync(systems, commonAppSetting, modelCompileInfo)
            return logSet :> ILogSet
        }
    static member InitializeLogReaderOnDemandAsync(querySet:QuerySet, systems:DsSystem seq) =
        task {
            Log4NetWrapper.logWithTrace <- true
            let! logSet = DBLoggerImpl.Reader.initializeLogReaderOnDemandAsync(querySet, systems)
            return logSet :> ILogSet
        }

    /// 조회 기간 변경 (reader)
    /// call site 에서는 기존 인자로 주어진 logSet 은 자동 dispose 되며, 새로 return 되는 logSet 을 이용하여야 한다.
    [<Obsolete("Not yet implemented")>]
    static member ChangeQueryDurationAsync(logSet:ILogSet, startAt:Nullable<DateTime>, endAt:Nullable<DateTime>) =
        task {
            let logSet = logSet :?> LogSet
            let querySet = QuerySet(startAt, endAt)
            let! newLogSet = DBLoggerImpl.Reader.changeQueryDurationAsync(logSet, querySet)
            dispose (logSet :> IDisposable)
            return newLogSet :> ILogSet
        }

    // { unit test 등의 debugging 용
    static member internal Count       (fqdns:string seq, tagKinds:int seq, logSet:LogSet) = DBLoggerImpl.count(logSet, fqdns, tagKinds, true)
    static member internal Count       (fqdn:string, tagKind:int, logSet:LogSet)           = DBLogger.Count([|fqdn|], [|tagKind|], logSet)
    static member internal GetLastValue(fqdn:string, tagKind:int, logSet:LogSet)           = DBLoggerImpl.getLastValue(logSet, fqdn, tagKind).Value
    static member internal Sum         (fqdn, tagKind, logSet:LogSet)                      = DBLoggerQueryImpl.sum(logSet, fqdn, tagKind)
    static member internal Average     (fqdn, tagKind, logSet:LogSet)                      = DBLoggerQueryImpl.average(logSet, fqdn, tagKind)
    // }

    static member Count(fqdns:string seq, tagKinds:int seq) = DBLoggerImpl.count(DBLoggerImpl.logSet, fqdns, tagKinds, true)
    static member Count(fqdn:string, tagKind:int) = DBLogger.Count([|fqdn|], [|tagKind|])
    static member GetLastValue(fqdn:string, tagKind:int) = DBLoggerImpl.getLastValue(DBLoggerImpl.logSet, fqdn, tagKind) |> Option.toNullable
    

    static member Sum     (fqdn, tagKind) = DBLoggerQueryImpl.sum(DBLoggerImpl.logSet, fqdn, tagKind)
    static member Average (fqdn, tagKind) = DBLoggerQueryImpl.average(DBLoggerImpl.logSet, fqdn, tagKind)

    static member GetDsFilePath (connectionString:string) = DBLoggerImpl.queryPropertyDsConfigJsonPathWithConnectionStringAsync(connectionString).Result

