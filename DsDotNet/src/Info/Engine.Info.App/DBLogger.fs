namespace Engine.Info

open Engine.Core
open Dual.Common.Core.FS
open System
open DBLoggerImpl

type DBLogger() =
    static let querySet = QuerySet((DateTime.MinValue, DateTime.MaxValue))

    static member EnqueLogForInsert(log:DsLog) = DBLoggerImpl.enqueLogForInsert(log:DsLog)
    static member EnqueLogsForInsert(logs:DsLog seq) = DBLoggerImpl.enqueLogsForInsert(logs:DsLog seq)
    static member InitializeLogWriterOnDemandAsync(systems:DsSystem seq, connectionString:string, pptPath:string, config:string) =
        task {
            let! logSet = DBLoggerImpl.initializeLogWriterOnDemandAsync(systems, connectionString, pptPath, config)
            return logSet :> IDisposable
        }
    static member InitializeLogReaderOnDemandAsync(systems:DsSystem seq, connectionString:string) =
        task {
            let! logSet = DBLoggerImpl.initializeLogReaderOnDemandAsync(querySet, systems, connectionString)
            return logSet :> IDisposable
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

    static member GetDsFilePath (connectionString:string) = DBLoggerImpl.getDsFilePath(connectionString)

