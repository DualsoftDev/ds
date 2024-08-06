namespace Engine.Info

open System
open Engine.Core
open Dual.Common.Base.FS
open Dual.Common.Core.FS
open Newtonsoft.Json


type ILogSet =
    inherit IDisposable

[<Flags>]
type DBLoggerType =
    | None = 0
    | Writer = 1
    | Reader = 2

type internal NewtonsoftJson = Newtonsoft.Json.JsonConvert


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

        let ModelRuntime = "modelRuntime"
        let ModelFilePath = "modelFilePath"


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
    , [runtime]     NVARCHAR(128) NOT NULL CHECK(runtime IN ('PC', 'PCSIM', 'PLC', 'PLCSIM'))       -- , 'LightPC', 'LightPLC', 'Simulation', 'Developer'
);

CREATE TABLE [{Tn.Log}] (
    [id]            INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
    , [storageId]   INTEGER NOT NULL
    , [at]          TEXT NOT NULL       -- SQLite DateTime 지원 안함.  DATETIME2(7)
    , [value]       NUMERIC NOT NULL
    , [modelId]     INTEGER NOT NULL
    , [token]       INTEGER             -- SEQ token
    , FOREIGN KEY(storageId) REFERENCES {Tn.Storage}(id)
);


CREATE TABLE [{Tn.TagKind}] (
    [id]            INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
    , [name]        NVARCHAR(64) NOT NULL CHECK(LENGTH(name) <= 64)
    -- , [modelId]     INTEGER NOT NULL
    , CONSTRAINT uniq_row UNIQUE (id, name)     -- , modelId
);

CREATE TABLE [{Tn.Property}] (
    [id]            INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
    , [name]        NVARCHAR(64) UNIQUE NOT NULL CHECK(LENGTH(name) <= 64)
    , [value]       NVARCHAR(64) NOT NULL CHECK(LENGTH(name) <= 64)
    , [modelId]     INTEGER     -- nullable. null 인 경우, 특정 모델에 대한 속성이 아님
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
        , log.[modelId] AS modelId
        , log.[token] AS token
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
        , stg.[modelId] AS modelId
    FROM [{Tn.Storage}] stg
    JOIN [{Tn.TagKind}] tagKind
    ON [stg].[tagKind] = [tagKind].[id]
    ;


"""

    type IDBRow = interface end
    let private nullToken = Nullable<uint32>()


    /// DB storage table 의 row 항목
    type ORMStorage(id:int, name: string, fqdn:string, tagKind:int, dataType:string, modelId:int) =
        new() = ORMStorage(-1, null, null, -1, null, -1)
        new(iStorage: IStorage) =
            if iStorage.Target.IsNone then failwithf $"Storage Target is not exist {iStorage.Name}"
            ORMStorage(-1, iStorage.Name, iStorage.Target.Value.QualifiedName, iStorage.TagKind, iStorage.DataType.Name, -1)

        interface IDBRow
        member val Id = id with get, set
        member val Name = name with get, set
        member val Fqdn = fqdn with get, set
        member val TagKind = tagKind with get, set
        member val DataType = dataType with get, set
        member val ModelId = modelId with get, set

    /// DB log table 의 row 항목
    type ORMLog(id: int, storageId: int, at: DateTime, value: obj, modelId:int, token:Nullable<uint32>) =
        do
            let x = 1
            ()

        new() = ORMLog(-1, -1, DateTime.MaxValue, null, -1, nullToken)

        interface IDBRow
        member val Id = id with get, set
        member val StorageId = storageId with get, set
        member val At = at with get, set
        member val Value = value with get, set
        member val ModelId = modelId with get, set
        member val Token = token with get, set
        static member private settings =
            let settings = new JsonSerializerSettings(TypeNameHandling = TypeNameHandling.All)
            settings.Converters.Insert(0, new ObjTypePreservingConverter([|"Value"|]))
            settings
        static member Deserialize(json: string) = NewtonsoftJson.DeserializeObject<ORMLog>(json, ORMLog.settings)
        member x.Serialize():string = NewtonsoftJson.SerializeObject(x, ORMLog.settings)


    /// DB log table 의 row 항목
    type ORMModel(id: int, path:string) =
        new() = ORMModel(-1, null)

        interface IDBRow
        member val Id = id with get, set
        member val Path = path with get, set

    /// tagKind table row
    type ORMTagKind() =
        interface IDBRow
        member val Id = 0 with get, set
        member val Name = "" with get, set

    /// Runtime 생성 log 항목
    type Log(id: int, storage: ORMStorage, at: DateTime, value: obj, modelId:int, token:Nullable<uint32>) =
        inherit ORMLog(id, storage.Id, at, value, modelId, token)
        new() = Log(-1, getNull<ORMStorage> (), DateTime.MaxValue, null, -1, nullToken)

        interface IDBRow
        member val Storage = storage with get, set
        member val At = at with get, set
        member val Value: obj = value with get, set

    type ORMVwLog(logId: int, storageId: int, name: string, fqdn: string, tagKind: int, tagKindName:string, at: DateTime, value: obj, modelId:int, token:Nullable<uint32>) =
        inherit ORMLog(logId, storageId, at, value, modelId, token)
        new() = ORMVwLog(-1, -1, null, null, -1, null, DateTime.MaxValue, null, -1, nullToken)
        member val Name = name with get, set
        member val Fqdn = fqdn with get, set
        member val TagKind = tagKind with get, set
        member val TagKindName = tagKindName with get, set



    type ORMProperty(id:int, name: string, value: string, modelId:int) =
        new() = ORMProperty(-1, null, null, -1)

        interface IDBRow
        member val Id = id with get, set
        member val Name = name with get, set
        member val Value = value with get, set
        member val ModelId = modelId with get, set


