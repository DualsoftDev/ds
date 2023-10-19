namespace Engine.Info

open Engine.Core

type DBLogger() =
    static member CreateLoggerDBSchema() = DBLoggerImpl.createLoggerDBSchema()
    static member CountFromDBAsync(fqdn:string, tagKind:int) = DBLoggerImpl.countFromDBAsync(fqdn, tagKind, true)
    static member CountFromDB(fqdn:string, tagKind:int) = DBLogger.CountFromDBAsync(fqdn, tagKind).Result
    static member InsertDBLogAsync(log:DsLog) = DBLoggerImpl.insertDBLogAsync(log:DsLog)
    static member InitializeOnDemandAsync(systems:DsSystem seq) = DBLoggerImpl.initializeOnDemandAsync(systems)


    static member  CollectDurationsONAsync(fqdn, tagKind) =
        use conn = createConnection()
        DBLoggerQueryImpl.collectDurationsONAsync(conn, fqdn, tagKind)

    static member  GetAverageONDurationAsync(fqdn, tagKind) =
        use conn = createConnection()
        DBLoggerQueryImpl.getAverageONDurationAsync(conn, fqdn, tagKind)

    static member GetAverageONDurationSeconds(fqdn, tagKind) =
        DBLogger.GetAverageONDurationAsync(fqdn, tagKind).Result.TotalSeconds

