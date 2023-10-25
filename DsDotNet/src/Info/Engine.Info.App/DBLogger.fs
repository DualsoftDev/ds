namespace Engine.Info

open Engine.Core
open Dual.Common.Core.FS

type DBLogger() =
    let logSetSelector (logSet:LoggerInfoSet) = if isItNull(logSet) then DBLoggerImpl.loggerInfo else logSet
    static member CreateLoggerDBSchema(connectionString) = DBLoggerImpl.createLoggerDBSchema(connectionString)
    static member EnqueLogForInsert(log:DsLog) = DBLoggerImpl.enqueLogForInsert(log:DsLog)
    static member EnqueLogsForInsert(logs:DsLog seq) = DBLoggerImpl.enqueLogsForInsert(logs:DsLog seq)
    static member InitializeLogWriterOnDemandAsync(systems:DsSystem seq) = DBLoggerImpl.initializeLogWriterOnDemandAsync(systems)
    static member InitializeLogReaderOnDemandAsync(systems:DsSystem seq) = DBLoggerImpl.initializeLogReaderOnDemandAsync(systems)

    // { unit test 등의 debugging 용
    static member internal CountLog(fqdns:string seq, tagKinds:int seq, logSet:LoggerInfoSet) = DBLoggerImpl.countLog(logSet, fqdns, tagKinds, true)
    static member internal CountLog(fqdn:string, tagKind:int, logSet:LoggerInfoSet) = DBLogger.CountLog([|fqdn|], [|tagKind|], logSet)
    static member internal GetLastValue(fqdn:string, tagKind:int, logSet:LoggerInfoSet) = DBLoggerImpl.getLastValue(logSet, fqdn, tagKind).Value
    static member internal CollectONDurations(fqdn, tagKind, logSet:LoggerInfoSet) = DBLoggerQueryImpl.collectONDurations(logSet, fqdn, tagKind)
    static member internal GetAverageONDuration(fqdn, tagKind, logSet:LoggerInfoSet) = DBLoggerQueryImpl.getAverageONDuration(logSet, fqdn, tagKind)  |> Option.toNullable
    // }

    static member CountLog(fqdns:string seq, tagKinds:int seq) = DBLoggerImpl.countLog(DBLoggerImpl.loggerInfo, fqdns, tagKinds, true)
    static member CountLog(fqdn:string, tagKind:int) = DBLogger.CountLog([|fqdn|], [|tagKind|])
    static member GetLastValue(fqdn:string, tagKind:int) = DBLoggerImpl.getLastValue(DBLoggerImpl.loggerInfo, fqdn, tagKind).Value
    

    static member CollectONDurations   (fqdn, tagKind) = DBLoggerQueryImpl.collectONDurations(DBLoggerImpl.loggerInfo, fqdn, tagKind)
    static member GetAverageONDuration (fqdn, tagKind) = DBLoggerQueryImpl.getAverageONDuration(DBLoggerImpl.loggerInfo, fqdn, tagKind) |> Option.toNullable
    static member GetAverageONDurationSeconds(fqdn, tagKind) =
        DBLoggerQueryImpl.getAverageONDuration(DBLoggerImpl.loggerInfo, fqdn, tagKind)
        |> Option.map(fun d -> d.TotalSeconds)
        |> Option.toNullable

