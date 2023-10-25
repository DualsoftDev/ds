namespace Engine.Info

open Engine.Core

type DBLogger() =
    static member CreateLoggerDBSchema() = DBLoggerImpl.createLoggerDBSchema()
    static member CountFromDBAsync(fqdns:string seq, tagKinds:int seq) = DBLoggerImpl.countFromDBAsync(fqdns, tagKinds, true)
    static member CountFromDB(fqdn:string, tagKind:int) = DBLogger.CountFromDBAsync([|fqdn|], [|tagKind|]).Result
    static member InsertDBLogAsync(log:DsLog) = DBLoggerImpl.insertDBLogAsync(log:DsLog)
    static member InsertDBLogsAsync(logs:DsLog seq) = DBLoggerImpl.insertDBLogsAsync(logs:DsLog seq)
    static member InitializeLogWriterOnDemandAsync(systems:DsSystem seq) = DBLoggerImpl.initializeLogWriterOnDemandAsync(systems)
    static member InitializeLogReaderOnDemandAsync(systems:DsSystem seq) = DBLoggerImpl.initializeLogReaderOnDemandAsync(systems)
    static member GetLastValueFromDBAsync(fqdn:string, tagKind:int) = DBLoggerImpl.GetLastValueFromDBAsync(fqdn, tagKind)
    

    static member  CollectDurationsONAsync(fqdn, tagKind) =
        use conn = createConnection()
        DBLoggerQueryImpl.collectDurationsONAsync(conn, fqdn, tagKind)

    static member  GetAverageONDurationAsync(fqdn, tagKind) =
        use conn = createConnection()
        DBLoggerQueryImpl.getAverageONDurationAsync(conn, fqdn, tagKind)

    static member GetAverageONDurationSeconds(fqdn, tagKind) =
        DBLogger.GetAverageONDurationAsync(fqdn, tagKind).Result.TotalSeconds

