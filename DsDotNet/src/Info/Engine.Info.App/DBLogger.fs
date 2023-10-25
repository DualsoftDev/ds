namespace Engine.Info

open Engine.Core

type DBLogger() =
    static member CreateLoggerDBSchema() = DBLoggerImpl.createLoggerDBSchema()
    static member CountLog(fqdns:string seq, tagKinds:int seq) = DBLoggerImpl.countLog(DBLoggerImpl.loggerInfo, fqdns, tagKinds, true)
    static member CountLog(fqdn:string, tagKind:int) = DBLogger.CountLog([|fqdn|], [|tagKind|])
    static member EnqueLogForInsert(log:DsLog) = DBLoggerImpl.enqueLogForInsert(log:DsLog)
    static member EnqueLogsForInsert(logs:DsLog seq) = DBLoggerImpl.enqueLogsForInsert(logs:DsLog seq)
    static member InitializeLogWriterOnDemandAsync(systems:DsSystem seq) = DBLoggerImpl.initializeLogWriterOnDemandAsync(systems)
    static member InitializeLogReaderOnDemandAsync(systems:DsSystem seq) = DBLoggerImpl.initializeLogReaderOnDemandAsync(systems)
    static member GetLastValue(fqdn:string, tagKind:int) = DBLoggerImpl.getLastValue(DBLoggerImpl.loggerInfo, fqdn, tagKind).Value
    

    static member  CollectDurationsONAsync(fqdn, tagKind) =
        use conn = createConnection()
        DBLoggerQueryImpl.collectDurationsON(DBLoggerImpl.loggerInfo, fqdn, tagKind)

    static member  GetAverageONDurationAsync(fqdn, tagKind) =
        use conn = createConnection()
        DBLoggerQueryImpl.getAverageONDurationAsync(DBLoggerImpl.loggerInfo, fqdn, tagKind)

    static member GetAverageONDurationSeconds(fqdn, tagKind) =
        DBLogger.GetAverageONDurationAsync(fqdn, tagKind).Result.TotalSeconds

