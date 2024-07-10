namespace Engine.Info

open System
open System.Configuration
open System.Collections.Concurrent
open Dapper
open Engine.Core
open Microsoft.Data.Sqlite
open Dual.Common.Core.FS
open Dual.Common.Db
open System.Collections.Generic
open System.Data
open System.Reactive.Disposables
open System.Threading.Tasks
open DBLoggerORM

[<AutoOpen>]
module internal DBLoggerTestModule =
    let createTestLoggerInfoSetForReader (queryCriteria: QueryCriteria, storages: Storage seq, ormLogs: ORMLog seq) : LogSet =
        let systems = []
        let logSet = new LogSet(queryCriteria, systems, storages, DBLoggerType.Reader)
        logSet.InitializeForReader(ormLogs)
        logSet
