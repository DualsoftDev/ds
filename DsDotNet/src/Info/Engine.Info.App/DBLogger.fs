namespace Engine.Info

open Engine.Core
open Dual.Common.Core.FS

type DBLogger() =
    let logSetSelector (logSet:LogSet) = if isItNull(logSet) then DBLoggerImpl.logSet else logSet
    static member CreateLoggerDBSchema(connectionString) = DBLoggerImpl.createLoggerDBSchema(connectionString)
    static member EnqueLogForInsert(log:DsLog) = DBLoggerImpl.enqueLogForInsert(log:DsLog)
    static member EnqueLogsForInsert(logs:DsLog seq) = DBLoggerImpl.enqueLogsForInsert(logs:DsLog seq)
    static member InitializeLogWriterOnDemandAsync(systems:DsSystem seq) = DBLoggerImpl.initializeLogWriterOnDemandAsync(systems)
    static member InitializeLogReaderOnDemandAsync(systems:DsSystem seq) = DBLoggerImpl.initializeLogReaderOnDemandAsync(systems)

    // { unit test 등의 debugging 용
    static member internal CountLog(fqdns:string seq, tagKinds:int seq, logSet:LogSet) = DBLoggerImpl.countLog(logSet, fqdns, tagKinds, true)
    static member internal CountLog(fqdn:string, tagKind:int, logSet:LogSet) = DBLogger.CountLog([|fqdn|], [|tagKind|], logSet)
    static member internal GetLastValue(fqdn:string, tagKind:int, logSet:LogSet) = DBLoggerImpl.getLastValue(logSet, fqdn, tagKind).Value
    static member internal CollectONDurations(fqdn, tagKind, logSet:LogSet) = DBLoggerQueryImpl.collectONDurations(logSet, fqdn, tagKind)
    static member internal GetAverageONDuration(fqdn, tagKind, logSet:LogSet) = DBLoggerQueryImpl.getAverageONDuration(logSet, fqdn, tagKind)  |> Option.toNullable
    // }

    static member CountLog(fqdns:string seq, tagKinds:int seq) = DBLoggerImpl.countLog(DBLoggerImpl.logSet, fqdns, tagKinds, true)
    static member CountLog(fqdn:string, tagKind:int) = DBLogger.CountLog([|fqdn|], [|tagKind|])
    static member GetLastValue(fqdn:string, tagKind:int) = DBLoggerImpl.getLastValue(DBLoggerImpl.logSet, fqdn, tagKind) |> Option.toNullable
    

    static member CollectONDurations   (fqdn, tagKind) = DBLoggerQueryImpl.collectONDurations(DBLoggerImpl.logSet, fqdn, tagKind)
    static member GetAverageONDuration (fqdn, tagKind) = DBLoggerQueryImpl.getAverageONDuration(DBLoggerImpl.logSet, fqdn, tagKind) |> Option.toNullable
    static member GetAverageONDurationSeconds(fqdn, tagKind) =
        DBLoggerQueryImpl.getAverageONDuration(DBLoggerImpl.logSet, fqdn, tagKind)
        |> Option.map(fun d -> d.TotalSeconds)
        |> Option.toNullable

