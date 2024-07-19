namespace Engine.Info

open DBLoggerORM

[<AutoOpen>]
module internal DBLoggerTestModule =
    let createTestLoggerInfoSetForReader (queryCriteria: QueryCriteria, storages: ORMStorage seq, ormLogs: ORMLog seq) : LogSet =
        let systems = []
        let logSet = new LogSet(queryCriteria, systems, storages, DBLoggerType.Reader)
        logSet.InitializeForReader(ormLogs)
        logSet
