namespace Engine.Info

open System
open Microsoft.Data.Sqlite
open Engine.Core
open Dual.Common.Base.FS
open Dual.Common.Core.FS
open System.Collections.Generic
open System.Reactive.Disposables
open System.Data
open Dapper
open Dual.Common.Db
open Newtonsoft.Json


type ILogSet =
    inherit IDisposable

[<Flags>]
type DBLoggerType =
    | None = 0
    | Writer = 1
    | Reader = 2

type internal NewtonsoftJson = Newtonsoft.Json.JsonConvert

/// DB logging query 기준
/// StartTime: 조회 시작 기간.
/// Null 이면 사전 지정된 start time 을 사용.  (사전 지정된 값이 없을 경우, DateTime.MinValue 와 동일)
/// 모든 데이터 조회하려면 DateTime.MinValue 를 사용
[<AllowNullLiteral>]
type QueryCriteria(commonAppSettings:DSCommonAppSettings, modelId:int, startAt: DateTime option, endAt: DateTime option) =
    //new() = QuerySet(getNull<DSCommonAppSettings>(), -1, None, None)

    new(commonAppSettings, modelId, startAt: Nullable<DateTime>, endAt: Nullable<DateTime>) =
        QueryCriteria(commonAppSettings, modelId, startAt |> Option.ofNullable, endAt |> Option.ofNullable)

    member x.ModelId = modelId
    /// 사용자 지정: 조회 start time
    member x.TargetStart = startAt
    /// 사용자 지정: 조회 end time
    member x.TargetEnd = endAt
    member val StartTime = startAt |? DateTime.MinValue with get, set
    member val EndTime = endAt |? DateTime.MaxValue with get, set
    member val CommonAppSettings = commonAppSettings
    member val DsConfigJsonPath = "" with get, set

[<AutoOpen>]
module DBLoggerORM =
    // database table names
    module Tn =
        let Storage = "storage"
        let Model = "model"
        let Log = "log"
        let Error = "error"
        let TagKind = "tagKind"
        let Property = "property"
        let User = "user"
    // database view names
    module Vn =
        let Log = "vwLog"
        let Storage = "vwStorage"

    // property table 사전 정의 row name
    module PropName =
        let PptPath = "ppt"
        let ConfigPath = "ds"
        let Start = "start"
        let End = "end"


    let sqlCreateSchema =
        $"""
CREATE TABLE [{Tn.Storage}] (
    [id]            INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
    , [name]        NVARCHAR(128) NOT NULL CHECK(LENGTH(name) <= 128)
    , [fqdn]        NVARCHAR(128) NOT NULL CHECK(LENGTH(fqdn) <= 128)
    , [tagKind]     INTEGER NOT NULL
    , [dataType]    NVARCHAR(64) NOT NULL CHECK(LENGTH(dataType) <= 64)
    , [modelId]     INTEGER NOT NULL
    , CONSTRAINT uniq_fqdn UNIQUE (fqdn, tagKind, dataType, name, modelId)
);

CREATE TABLE [{Tn.Model}] (
    [id]            INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
    , [path]        NVARCHAR(128) NOT NULL  -- pptx/zip path
    , [lastModified] DATETIME2(7) NOT NULL  -- file last modified
    , [runtime]     NVARCHAR(128) NOT NULL CHECK(runtime IN ('PC', 'PLC', 'LightPC', 'LightPLC', 'Simulation', 'Developer'))
);

CREATE TABLE [{Tn.Log}] (
    [id]            INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
    , [storageId]   INTEGER NOT NULL
    , [at]          DATETIME2(7) NOT NULL
    , [value]       NUMERIC NOT NULL
    , [modelId]     INTEGER NOT NULL
    , FOREIGN KEY(storageId) REFERENCES {Tn.Storage}(id)
);


CREATE TABLE [{Tn.TagKind}] (
    [id]            INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
    , [name]        NVARCHAR(64) UNIQUE NOT NULL CHECK(LENGTH(name) <= 64)
    , [modelId]     INTEGER NOT NULL
    , CONSTRAINT uniq_row UNIQUE (id, name, modelId)
);

CREATE TABLE [{Tn.Property}] (
    [id]            INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
    , [name]        NVARCHAR(64) UNIQUE NOT NULL CHECK(LENGTH(name) <= 64)
    , [value]       NVARCHAR(64) NOT NULL CHECK(LENGTH(name) <= 64)
    , [modelId]     INTEGER NOT NULL
);


CREATE TABLE [{Tn.User}] (
    [id]            INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
    , [username]    NVARCHAR(64) NOT NULL       CHECK(LENGTH(username) <= 64)
    , [password]    NVARCHAR(512)               CHECK(LENGTH(password) <= 512) -- NOT NULL
    , [isAdmin]     TINYINT NOT NULL DEFAULT 0
    , [roles]       NVARCHAR(512)                -- "Administrator" role 은 여기에 포함되지 않음.
    , CONSTRAINT user_name_uniq UNIQUE (username)
);

INSERT INTO [{Tn.User}]
    (username, password, isAdmin)
    VALUES ('admin', null, 1);


CREATE VIEW [{Vn.Log}] AS
    SELECT
        log.[id] AS id
        , log.[storageId] AS storageId
        , stg.[name] AS name
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

CREATE VIEW [{Vn.Storage}] AS
    SELECT
        stg.[id] AS id
        , stg.[name] AS name
        , stg.[fqdn] AS fqdn
        , tagKind.[id] AS tagKind
        , tagKind.[name] AS tagKindName
        , stg.[dataType] AS dataType
    FROM [{Tn.Storage}] stg
    JOIN [{Tn.TagKind}] tagKind
    ON [stg].[tagKind] = [tagKind].[id]
    ;


"""

    type IDBRow = interface end

    /// DB storage table 의 row 항목
    type Storage(id: int, tagKind: int, fqdn: string, dataTypeName: string, name: string) =
        new() = Storage(-1, -1, null, null, null)

        new(iStorage: IStorage) =
            if iStorage.Target.IsNone then failwithf $"Storage Target is not exist {iStorage.Name}"
            Storage(-1, iStorage.TagKind, iStorage.Target.Value.QualifiedName, iStorage.DataType.Name, iStorage.Name)

        interface IDBRow
        member val Id: int = id with get, set
        member val Fqdn = fqdn with get, set
        member val TagKind = tagKind with get, set
        member val DataType = dataTypeName with get, set
        member val Name = name with get, set

    /// DB log table 의 row 항목
    type ORMLog(id: int, storageId: int, at: DateTime, value: obj) =
        do
            let x = 1
            ()

        new() = ORMLog(-1, -1, DateTime.MaxValue, null)

        interface IDBRow
        member val Id = id with get, set
        member val StorageId = storageId with get, set
        member val At = at with get, set
        member val Value = value with get, set
        static member private settings =
            let settings = new JsonSerializerSettings(TypeNameHandling = TypeNameHandling.All)
            settings.Converters.Insert(0, new ObjTypePreservingConverter([|"Value"|]))
            settings
        static member Deserialize(json: string) = NewtonsoftJson.DeserializeObject<ORMLog>(json, ORMLog.settings)
        member x.Serialize():string = NewtonsoftJson.SerializeObject(x, ORMLog.settings)


    /// DB log table 의 row 항목
    type ORMModel(id: int, path:string, lastModified:DateTime) =
        new() = ORMModel(-1, null, DateTime.MinValue)

        interface IDBRow
        member val Id = id with get, set
        member val Path = path with get, set
        member val LastModified = lastModified with get, set

    /// tagKind table row
    type ORMTagKind() =
        interface IDBRow
        member val Id = 0 with get, set
        member val Name = "" with get, set

    /// Runtime 생성 log 항목
    type Log(id: int, storage: Storage, at: DateTime, value: obj) =
        inherit ORMLog(id, storage.Id, at, value)
        new() = Log(-1, getNull<Storage> (), DateTime.MaxValue, null)

        interface IDBRow
        member val Storage = storage with get, set
        member val At = at with get, set
        member val Value: obj = value with get, set

    type ORMVwLog(logId: int, storageId: int, name: string, fqdn: string, tagKind: int, tagKindName:string, at: DateTime, value: obj) =
        inherit ORMLog(logId, storageId, at, value)
        new() = ORMVwLog(-1, -1, null, null, -1, null, DateTime.MaxValue, null)
        member val Name = name with get, set
        member val Fqdn = fqdn with get, set
        member val TagKind = tagKind with get, set
        member val TagKindName = tagKindName with get, set



    type ORMProperty(id:int, name: string, value: string) =
        new() = ORMProperty(-1, null, null)

        interface IDBRow
        member val Id = id with get, set
        member val Name = name with get, set
        member val Value = value with get, set

    type ORMStorage(id:int, name: string, fqdn:string, tagKind:int, dataType:string, modelId:int) =
        new() = ORMStorage(-1, null, null, -1, null, -1)

        interface IDBRow
        member val Id = id with get, set
        member val Name = name with get, set
        member val Fqdn = fqdn with get, set
        member val TagKind = tagKind with get, set
        member val DataType = dataType with get, set
        member val ModelId = modelId with get, set


    type Fqdn = string
    type StorageKey = TagKind * Fqdn

    let getStorageKey (s: Storage) : StorageKey = s.TagKind, s.Fqdn

    /// StorageKey(-> TagKind*Fqdn) 로 주어진 항목에 대한 summary (-> Count, Sum)
    type Summary(logSet: LogSet, storageKey: StorageKey, count: int, sum: double) =
        let mutable count = count
        let mutable sum = sum
        /// Number rising
        member x.Count
            with get() = count
            // todo: 여기서 notify info
            and set(v) = count <- v
        /// Duration sum (sec 단위)
        member x.Sum
            with get() = sum
            // todo: 여기서 notify info
            and set(v) = sum <- v
        /// Container reference
        member x.LogSet = logSet
        member x.StorageKey = storageKey
        member val LastLog: Log option = None with get, set

    /// DB logging 관련 전체 설정
    and LogSet(queryCriteria: QueryCriteria, systems: DsSystem seq, storages: Storage seq, readerWriterType: DBLoggerType) as this =
        let storageDic = storages |> map (fun s -> getStorageKey s, s) |> Tuple.toDictionary

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
        member val QuerySet = queryCriteria with get, set
        member x.Summaries = summaryDic
        member x.Storages = storageDic
        member x.StoragesById = storageByIdDic
        member val LastLog: Log option = None with get, set
        member x.ReaderWriterType = readerWriterType
        member x.Disposables = disposables
        member x.GetSummary(summaryKey: StorageKey) = summaryDic[summaryKey]

        interface ILogSet with
            override x.Dispose() = x.Disposables.Dispose()



    /// property table 항목 조회
    let queryPropertyAsync (modelId:int, propertyName: string, conn: IDbConnection, tr: IDbTransaction) =
        conn.QueryFirstOrDefaultAsync<string>(
            $"SELECT value FROM [{Tn.Property}] WHERE name = @Name AND modelId=@ModelId",
            {| Name = propertyName; ModelId=modelId |},
            tr
        )

    /// property table 항목 수정
    let updatePropertyAsync (modelId:int, propertyName: string, value: string, conn: IDbConnection, tr: IDbTransaction) =
        conn.ExecuteSilentlyAsync(
            $"""INSERT OR REPLACE INTO [{Tn.Property}]
                (name, value, modelId)
                VALUES(@Name, @Value, @ModelId);""",
            {| Name = propertyName; Value = value; ModelId=modelId |},
            tr
        )


    type QueryCriteria with

        /// 조회 기간 target 설정 값 필요시 db 에 반영하고, target 에 맞게 조회 기간 변경
        member x.SetQueryRangeAsync(modelId:int, conn: IDbConnection, tr: IDbTransaction) =
            task {
                match x.TargetStart with
                | Some s ->
                    do! updatePropertyAsync (modelId, PropName.Start, s.ToString(), conn, tr)
                    x.StartTime <- s
                | _ ->
                    let! str = queryPropertyAsync (modelId, PropName.Start, conn, tr)

                    x.StartTime <-
                        if isNull (str) then
                            DateTime.MinValue
                        else
                            DateTime.Parse(str)


                match x.TargetEnd with
                | Some e ->
                    do! updatePropertyAsync (modelId, PropName.End, e.ToString(), conn, tr)
                    x.EndTime <- e
                | _ ->
                    let! str = queryPropertyAsync (modelId, PropName.End, conn, tr)

                    x.EndTime <-
                        if isNull (str) then
                            DateTime.MaxValue
                        else
                            DateTime.Parse(str)

                logInfo $"Query range set: [{x.StartTime} ~ {x.EndTime}]"
            }


    let createConnectionWith (connStr) =
        new SqliteConnection(connStr) |> tee (fun conn -> conn.Open())

    type DSCommonAppSettings with
        member x.ConnectionString = x.LoggerDBSettings.ConnectionString
        member x.CreateConnection(): SqliteConnection = createConnectionWith x.ConnectionString
            
