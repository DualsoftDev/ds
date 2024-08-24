namespace Engine.Info

open System
open Engine.Core
open Dual.Common.Base.FS
open Dual.Common.Core.FS
open Newtonsoft.Json


type ILogSet = interface end

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
        let Storage  = "storage"
        let Model    = "model"
        let Log      = "log"
        let Token    = "token"
        let Error    = "error"
        let TagKind  = "tagKind"
        /// 예지 보전
        let Maintenance     = "maintenance"
        let Property = "property"
        let User     = "user"
    // database view names
    module Vn =
        let Log     = "vwLog"
        let Storage = "vwStorage"

    // property table 사전 정의 row name
    module PropName =
        let PptPath    = "ppt"
        let ConfigPath = "ds"
        let Start      = "start"
        let End        = "end"

        let ModelRuntime  = "modelRuntime"
        let ModelFilePath = "modelFilePath"


    (*
     * [Storage]
        +---------------------------------------+---------------------------+-----------+-----------+--------+---------------+
        | name                                  | fqdn                      | tagKind   | dataType  | modelId| maintenanceId
        +---------------------------------------+---------------------------+-----------+-----------+--------+---------------+
        | _ON                                   | SIDE11                    | 0         | Boolean   | 1      |               |       // TagKind.[ 0] = SystemTag._ON
        | _OFF                                  | SIDE11                    | 1         | Boolean   | 1      |               |       // TagKind.[ 1] = SystemTag._OFF
        | SIDE11_auto_btn                       | SIDE11                    | 2         | Boolean   | 1      |               |       // TagKind.[ 2] = SystemTag.auto_btn
        | SIDE11_auto_lamp                      | SIDE11                    | 12        | Boolean   | 1      |               |       // TagKind.[12] = SystemTag.auto_lamp
        | SIDE11_timeout                        | SIDE11                    | 27        | UInt32    | 1      |               |       // TagKind.[27] = SystemTag.timeout
        | SIDE11_MES_idle_mode                  | SIDE11.MES                | 10,100    | Boolean   | 1      |               |       // TagKind.[10100] = FlowTag.idle_mode
        | SIDE11_S200_CARTYPE_MOVE_idle_mode    | SIDE11.S200_CARTYPE_MOVE  | 10,100    | Boolean   | 1      |               |       // TagKind.[10100] = FlowTag.idle_mode
        +---------------------------------------+---------------------------+-----------+-----------+--------+---------------+
    *)
    let sqlCreateSchema =
        $"""
CREATE TABLE [{Tn.Storage}] (
    [id]            INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
    , [name]        NVARCHAR(128) NOT NULL CHECK(LENGTH(name) <= 128)
    , [fqdn]        NVARCHAR(128) NOT NULL CHECK(LENGTH(fqdn) <= 128)
    , [tagKind]     INTEGER NOT NULL    -- 값 자체가 tagKind table 의 id 이다.
    , [dataType]    NVARCHAR(64) NOT NULL CHECK(LENGTH(dataType) <= 64)
    , [modelId]     INTEGER NOT NULL
    , [maintenanceId]   INTEGER
    , CONSTRAINT uniq_fqdn UNIQUE (fqdn, tagKind, dataType, name, modelId)
    , FOREIGN KEY(tagKind) REFERENCES {Tn.TagKind}(id)
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
    , [modelId]     INTEGER NOT NULL    -- Storage.modelId 와 중복되나, join 피하기 위해서 중복 허용
    , [tokenId]     INTEGER             -- SEQ token. allow NULL
    , FOREIGN KEY(storageId) REFERENCES {Tn.Storage}(id)
    , FOREIGN KEY(tokenId) REFERENCES {Tn.Token}(id)
    , FOREIGN KEY(modelId) REFERENCES {Tn.Model}(id)
);

CREATE TABLE [{Tn.Token}] (
    [id]                INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
    , [at]              TEXT NOT NULL       -- SQLite DateTime 지원 안함.  DATETIME2(7)
    , [originalToken]   INTEGER NOT NULL    -- System 에서 발번한 원래의 token 번호 (not ID)
    , [mergedTokenId]   INTEGER             -- this token 이 병합되는 대상 turnk 의 tokenId
    , [modelId]         INTEGER NOT NULL
    , FOREIGN KEY(mergedTokenId) REFERENCES {Tn.Token}(id)  -- 자기 참조 외래 키(self-referencing foreign key)
);

CREATE TABLE [{Tn.TagKind}] (
    [id]            INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
    , [name]        NVARCHAR(64) NOT NULL CHECK(LENGTH(name) <= 64)
    -- , [modelId]     INTEGER NOT NULL
    , CONSTRAINT uniq_row UNIQUE (id, name)     -- , modelId
);

CREATE TABLE [{Tn.Maintenance}] (
    [id]            INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
    , [minDuration] INTEGER         -- ms 단위
    , [maxDuration] INTEGER         -- ms 단위
    , [maxNumOperation] INTEGER     -- 최대 가용 횟수.  부품 교체후 reset 필요
    , [modelId]     INTEGER NOT NULL    -- 편의상, 중복 허용.  참조키 check 는 제외 함.  코멘트 성격으로 사용
    , [storageId]   INTEGER NOT NULL    -- 편의상, 중복 허용.
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
        , log.[tokenId] AS tokenId
        , token.[originalToken] AS originalToken
        , token.[at] AS tokenAt
    FROM [{Tn.Log}] log
    JOIN [{Tn.Storage}] stg
    ON [stg].[id] = [log].[storageId]
    JOIN [{Tn.TagKind}] tagKind
    ON [stg].[tagKind] = [tagKind].[id]
    LEFT JOIN [{Tn.Token}] token
    ON [log].[tokenId] = [token].[id]
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
    type internal IdType = int64
    type internal TokenType = int64
    type internal DurationType = int64
    type internal CounterType = int64
    type internal NullableTokenType = Nullable<TokenType>
    type internal NullableDurationType = Nullable<DurationType>
    type internal NullableCounterType = Nullable<CounterType>
    type internal NullableIdType = Nullable<IdType>
    let internal nullDuration = NullableDurationType()
    let internal nullId = NullableIdType()
    let internal nullCounter = NullableCounterType()
    let internal nullToken = NullableTokenType()


    /// DB storage table 의 row 항목
    type ORMStorage(id:int, name: string, fqdn:string, tagKind:int, dataType:string, modelId:int, maintenanceId:NullableIdType, storage:IStorage) =
        new() = ORMStorage(-1, null, null, -1, null, -1, nullId, getNull<IStorage>())
        new(id, name, fqdn, tagKind, dataType) = ORMStorage(id, name, fqdn, tagKind, dataType, -1, nullId, getNull<IStorage>())
        new(iStorage: IStorage) =
            iStorage.Target.IsSome |> verifyM $"Storage Target is not exist {iStorage.Name}"
            let mainenanceId, modelId = nullId, -1
            ORMStorage(-1, iStorage.Name, iStorage.Target.Value.QualifiedName, iStorage.TagKind, iStorage.DataType.Name, modelId, mainenanceId, iStorage)

        interface IDBRow
        member val Id = id with get, set
        member val Name = name with get, set
        member val Fqdn = fqdn with get, set
        member val TagKind = tagKind with get, set
        member val DataType = dataType with get, set
        member val ModelId = modelId with get, set
        member val MaintenanceId = maintenanceId with get, set

        // ORM 제외 항목
        member val Storage:IStorage = storage with get, set


    type MaintenanceInfo(minDuration:NullableDurationType, maxDuration:NullableDurationType, maxNumOperation:NullableCounterType) =
        interface IMainenance with
            member x.MinDuration     with get() = x.MinDuration     and set v = x.MinDuration     <- v
            member x.MaxDuration     with get() = x.MaxDuration     and set v = x.MaxDuration     <- v
            member x.MaxNumOperation with get() = x.MaxNumOperation and set v = x.MaxNumOperation <- v

        member val MinDuration = minDuration with get, set
        member val MaxDuration = maxDuration with get, set
        member val MaxNumOperation = maxNumOperation with get, set

    type MaintenanceInfo with
        member x. TryGetMinDuration() = x.MinDuration.ToOption()
        member x. TryGetMaxDuration() = x.MaxDuration.ToOption()


    type ORMMaintenance(id:int, modelId:int, storageId:int, minDuration:NullableDurationType, maxDuration:NullableDurationType, maxNumOperation:NullableCounterType) =
        inherit MaintenanceInfo(minDuration, maxDuration, maxNumOperation)
        new() = ORMMaintenance(-1, -1, -1, nullDuration, nullDuration, nullCounter)
        interface IDBRow
        member val Id = id with get, set
        member val ModelId = modelId with get, set
        member val StorageId = storageId with get, set


    /// DB log table 의 row 항목
    type ORMLog(id: int, storageId: int, at: DateTime, value: obj, modelId:int, tokenId:TokenIdType) =
        new() = ORMLog(-1, -1, DateTime.MaxValue, null, -1, TokenIdType())

        interface IDBRow
        member val Id = id with get, set
        member val StorageId = storageId with get, set
        member val At = at with get, set
        member val Value = value with get, set
        member val ModelId = modelId with get, set
        member val TokenId = tokenId with get, set
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
    type Log(id: int, storage: ORMStorage, at: DateTime, value: obj, modelId:int, tokenId:TokenIdType) =
        inherit ORMLog(id, storage.Id, at, value, modelId, tokenId)
        new() = Log(-1, getNull<ORMStorage> (), DateTime.MaxValue, null, -1, nullId)

        interface IDBRow
        member val Storage = storage with get, set
        member val At = at with get, set
        member val Value: obj = value with get, set

    type ORMVwLog(logId: int, storageId: int, name: string, fqdn: string, tagKind: int, tagKindName:string, at: DateTime, value: obj, modelId:int, tokenId:TokenIdType) =
        inherit ORMLog(logId, storageId, at, value, modelId, tokenId)
        new() = ORMVwLog(-1, -1, null, null, -1, null, DateTime.MaxValue, null, -1, nullId)
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


