namespace Engine.Info

open System
open Engine.Core
open Dual.Common.Core.FS
open System.Collections.Generic
open System.Reactive.Disposables
open System.Data
open Dapper
open Dual.Common.Db


type ILogSet = inherit IDisposable

/// DB logging query 기준
/// StartTime: 조회 시작 기간.
/// Null 이면 사전 지정된 start time 을 사용.  (사전 지정된 값이 없을 경우, DateTime.MinValue 와 동일)
/// 모든 데이터 조회하려면 DateTime.MinValue 를 사용
[<AllowNullLiteral>]
type QuerySet(startAt:DateTime option, endAt:DateTime option) =
    new() = QuerySet(None, None)
    new(startAt:Nullable<DateTime>, endAt:Nullable<DateTime>) = QuerySet(startAt |> Option.ofNullable, endAt |> Option.ofNullable)
    /// 사용자 지정: 조회 start time
    member x.TargetStart = startAt
    /// 사용자 지정: 조회 end time
    member x.TargetEnd   = endAt
    member val StartTime = startAt |? DateTime.MinValue with get, set
    member val EndTime   = endAt   |? DateTime.MaxValue with get, set
    member val CommonAppSettings:DSCommonAppSettings = getNull<DSCommonAppSettings>() with get, set


[<AutoOpen>]
module internal DBLoggerORM =
    // database table names
    module Tn =
        let Storage = "storage"
        let Log = "log"
        let Error = "error"
        let TagKind = "tagKind"
        let Property = "property"
    // database view names
    module Vn =
        let Log = "vwLog"

    // property table 사전 정의 row name
    module PropName =
        let PptPath = "ppt"
        let ConfigPath = "ds"
        let Start = "start"
        let End = "end"


    let sqlCreateSchema = $"""
CREATE TABLE [{Tn.Storage}] (
    [id]            INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
    , [name]        NVARCHAR(64) NOT NULL CHECK(LENGTH(name) <= 64)
    , [fqdn]        NVARCHAR(64) NOT NULL CHECK(LENGTH(fqdn) <= 64)
    , [tagKind]     INTEGER NOT NULL
    , [dataType]    NVARCHAR(64) NOT NULL CHECK(LENGTH(dataType) <= 64)
    , CONSTRAINT uniq_fqdn UNIQUE (fqdn, tagKind, dataType, name)
);

CREATE TABLE [{Tn.Log}] (
    [id]            INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
    , [storageId]   INTEGER NOT NULL
    , [at]          DATETIME2(7) NOT NULL
    , [value]       NUMERIC NOT NULL
    , FOREIGN KEY(storageId) REFERENCES {Tn.Storage}(id)
);


CREATE TABLE [{Tn.TagKind}] (
    [id]            INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
    , [name]        NVARCHAR(64) UNIQUE NOT NULL CHECK(LENGTH(name) <= 64)
    , CONSTRAINT uniq_row UNIQUE (id, name)
);

CREATE TABLE [{Tn.Property}] (
    [id]            INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
    , [name]        NVARCHAR(64) UNIQUE NOT NULL CHECK(LENGTH(name) <= 64)
    , [value]       NVARCHAR(64) NOT NULL CHECK(LENGTH(name) <= 64)
);


CREATE VIEW [{Vn.Log}] AS
    SELECT
        log.[id] AS id
        , log.[storageId] AS storageId
        , stg.[fqdn] AS fqdn
        , stg.[tagKind] AS tagKind
        , tagKind.[name] AS tagKindName
        , log.[at] AS at
        , log.[value] AS value
    FROM [{Tn.Log}] log
    JOIN [{Tn.Storage}] stg
    ON [stg].[id] = [log].[storageId]
    JOIN [{Tn.TagKind}] tagKind
    ON [stg].[tagKind] = [tagKind].[id]
    ;
"""

    type Storage(id:int, tagKind:int, fqdn:string, dataTypeName:string, name:string) =
        new() = Storage(-1, -1, null, null, null)
        new(iStorage:IStorage) = Storage(-1, iStorage.TagKind, iStorage.Target.Value.QualifiedName, iStorage.DataType.Name, iStorage.Name)
        member val Id:int   = id           with get, set
        member val Fqdn     = fqdn         with get, set
        member val TagKind  = tagKind      with get, set
        member val DataType = dataTypeName with get, set
        member val Name     = name         with get, set

    type ORMLog(id:int, storageId:int, at:DateTime, value:obj) =
        new() = ORMLog(-1, -1, DateTime.MaxValue, null)
        member val Id = id with get, set
        member val StorageId = storageId with get, set
        member val At = at with get, set
        member val Value = value with get, set

    /// tagKind table row
    type ORMTagKind() =
        member val Id = 0 with get, set
        member val Name = "" with get, set

    type Log(id:int, storage:Storage, at:DateTime, value:obj) =
        inherit ORMLog(id , storage.Id, at, value)
        new() = Log(-1, getNull<Storage>(), DateTime.MaxValue, null)
        member val Storage   = storage with get, set
        member val At        = at      with get, set
        member val Value:obj = value   with get, set

    type TagKind = int
    type Fqdn = string
    type StorageKey = TagKind*Fqdn

    let getStorageKey(s:Storage):StorageKey = s.TagKind, s.Fqdn

    type Summary(logSet:LogSet, storageKey:StorageKey, count:int, sum:double) =
        /// Number rising
        member val Count = count with get, set
        /// Duration sum (sec 단위)
        member val Sum = sum with get, set
        /// Container reference
        member x.LogSet = logSet
        member x.StorageKey = storageKey
        member val LastLog:Log option = None with get, set
    
    and LogSet(querySet:QuerySet, systems:DsSystem seq, storages:Storage seq, isReader:bool) as this =
        let storageDic =
            storages
            |> map (fun s -> getStorageKey s, s)
            |> Tuple.toDictionary

        let summaryDic =
            storages
            |> map (fun s ->
                let key = getStorageKey s
                key, Summary(this, key, 0, 0.))
            |> Tuple.toDictionary

        let storageByIdDic = Dictionary<int, Storage>()
        let systems = systems |> toArray

        let disposables = new CompositeDisposable()

        member x.Systems = systems
        member val QuerySet = querySet with get, set
        member x.Summaries = summaryDic
        member x.Storages = storageDic
        member x.StoragesById = storageByIdDic
        member val LastLog:Log option = None with get, set
        member x.IsLogReader = isReader
        member x.Disposables = disposables
        member x.GetSummary(summaryKey:StorageKey) = summaryDic[summaryKey]

        interface ILogSet with
            override x.Dispose() = x.Disposables.Dispose()



    let queryPropertyAsync(propertyName:string, conn:IDbConnection, tr:IDbTransaction) =
        conn.QueryFirstOrDefaultAsync<string>($"SELECT value FROM [{Tn.Property}] WHERE name = @Name", {|Name=propertyName|}, tr)
    let updatePropertyAsync(propertyName:string, value:string, conn:IDbConnection, tr:IDbTransaction) =
        conn.ExecuteSilentlyAsync($"""INSERT OR REPLACE INTO [{Tn.Property}]
                                    (name, value)
                                    VALUES(@Name, @Value);"""
                                    , {|Name=propertyName; Value=value|}, tr)


    type QuerySet with
        member x.SetQueryRangeAsync(conn:IDbConnection, tr:IDbTransaction) =
            task {
                match x.TargetStart with
                | Some s ->
                    do! updatePropertyAsync(PropName.Start, s.ToString(), conn, tr)
                    x.StartTime <- s
                | _ ->
                    let! str = queryPropertyAsync(PropName.Start, conn, tr)
                    x.StartTime <- if isNull(str) then DateTime.MinValue else DateTime.Parse(str)


                match x.TargetEnd with
                | Some e ->
                    do! updatePropertyAsync(PropName.End, e.ToString(), conn, tr)
                    x.EndTime <- e
                | _ ->
                    let! str = queryPropertyAsync(PropName.End, conn, tr)
                    x.EndTime <- if isNull(str) then DateTime.MaxValue else DateTime.Parse(str)

                logInfo $"Query range set: [{x.StartTime} ~ {x.EndTime}]"
            }