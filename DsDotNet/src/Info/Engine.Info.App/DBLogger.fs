namespace Engine.Info

open Engine.Core

module DBLogger =
    let CreateLoggerDBSchema() = DBLoggerImpl.createLoggerDBSchema()
    let CountFromDBAsync(fqdn:string, tagKind:int) = DBLoggerImpl.countFromDBAsync(fqdn, tagKind, true)
    let CountFromDB(fqdn:string, tagKind:int) = CountFromDBAsync(fqdn, tagKind).Result
    let InsertDBLogAsync(log:DsLog) = DBLoggerImpl.insertDBLogAsync(log:DsLog)
    let InitializeOnDemandAsync(systems:DsSystem seq) = DBLoggerImpl.initializeOnDemandAsync(systems)


    let CollectDurationsONAsync(fqdn, tagKind) =
        use conn = createConnection()
        DBLoggerQueryImpl.collectDurationsONAsync(conn, fqdn, tagKind)

    let GetAverageONDurationAsync(fqdn, tagKind) =
        use conn = createConnection()
        DBLoggerQueryImpl.getAverageONDurationAsync(conn, fqdn, tagKind)

    let GetAverageONDurationSeconds(fqdn, tagKind) =
        GetAverageONDurationAsync(fqdn, tagKind).Result.TotalSeconds

