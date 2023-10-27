namespace Engine.Info

open Engine.Core
open Dual.Common.Core.FS
open System

type DBLogger() =
    static let querySet = QuerySet((DateTime.MinValue, DateTime.MaxValue))

    static member CreateLoggerDBSchema(connectionString) = DBLoggerImpl.createLoggerDBSchema(connectionString)
    static member EnqueLogForInsert(log:DsLog) = DBLoggerImpl.enqueLogForInsert(log:DsLog)
    static member EnqueLogsForInsert(logs:DsLog seq) = DBLoggerImpl.enqueLogsForInsert(logs:DsLog seq)
    static member InitializeLogWriterOnDemandAsync(systems:DsSystem seq) = DBLoggerImpl.initializeLogWriterOnDemandAsync(systems)
    static member InitializeLogReaderOnDemandAsync(systems:DsSystem seq) = DBLoggerImpl.initializeLogReaderOnDemandAsync(querySet, systems)

    // { unit test 등의 debugging 용
    static member internal CountLog(fqdns:string seq, tagKinds:int seq, logSet:LogSet) = DBLoggerImpl.countLog(logSet, fqdns, tagKinds, true)
    static member internal CountLog(fqdn:string, tagKind:int, logSet:LogSet) = DBLogger.CountLog([|fqdn|], [|tagKind|], logSet)
    static member internal GetLastValue(fqdn:string, tagKind:int, logSet:LogSet) = DBLoggerImpl.getLastValue(logSet, fqdn, tagKind).Value
    static member internal CollectONDurations(fqdn, tagKind, logSet:LogSet) = DBLoggerQueryImpl.sum(logSet, fqdn, tagKind)
    static member internal GetAverageONDuration(fqdn, tagKind, logSet:LogSet) = DBLoggerQueryImpl.average(logSet, fqdn, tagKind)
    // }

    static member CountLog(fqdns:string seq, tagKinds:int seq) = DBLoggerImpl.countLog(DBLoggerImpl.logSet, fqdns, tagKinds, true)
    static member CountLog(fqdn:string, tagKind:int) = DBLogger.CountLog([|fqdn|], [|tagKind|])
    static member GetLastValue(fqdn:string, tagKind:int) = DBLoggerImpl.getLastValue(DBLoggerImpl.logSet, fqdn, tagKind) |> Option.toNullable
    

    static member CollectONDurations   (fqdn, tagKind) = DBLoggerQueryImpl.sum(DBLoggerImpl.logSet, fqdn, tagKind)
    static member GetAverageONDuration (fqdn, tagKind) = DBLoggerQueryImpl.average(DBLoggerImpl.logSet, fqdn, tagKind)

