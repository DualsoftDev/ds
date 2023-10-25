namespace Engine.Info

open System
open System.Configuration;
open Dapper
open Engine.Core
open Microsoft.Data.Sqlite
open Dual.Common.Core.FS
open Dual.Common.Db
open System.Collections.Generic
open System.Data
open System.Reactive.Disposables


[<AutoOpen>]
module internal DBLoggerImpl =
    // database table names
    module Tn =
        let Storage = "storage"
        let Log = "log"
        let Error = "error"
    // database view names
    module Vn =
        let Log = "vwLog"

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

-- CREATE TABLE [{Tn.Error}] (
--     [id]            INTEGER PRIMARY KEY AUTOINCREMENT NOT NULL
--     , [logId]       INTEGER NOT NULL
--     , [message]     NVARCHAR(1024) NOT NULL CHECK(LENGTH(message) <= 1024)
--     , FOREIGN KEY(logId) REFERENCES {Tn.Log}(id)
-- );


CREATE VIEW [{Vn.Log}] AS
    SELECT
        log.[id] AS id
        , log.[storageId] AS storageId
        , stg.[fqdn] AS fqdn
        , stg.[tagKind] AS tagKind
        , log.[at] AS at
        , log.[value] AS value
    FROM [{Tn.Log}] log
    JOIN [{Tn.Storage}] stg
    ON [stg].[id] = [log].[storageId]
    ;
    """
    let connectionString = ConfigurationManager.ConnectionStrings["DBLoggerConnectionString"].ConnectionString;
    let createConnection() =
        new SqliteConnection(connectionString) |> tee (fun conn -> conn.Open())

    let createLoggerDBSchema() =
        use conn = createConnection()
        if not <| conn.IsTableExistsAsync(Tn.Log).Result then
            conn.Execute(sqlCreateSchema, null) |> ignore




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

    type Log(id:int, storage:Storage, at:DateTime, value:obj) =
        inherit ORMLog(id , storage.Id, at, value)
        new() = Log(-1, getNull<Storage>(), DateTime.MaxValue, null)
        member val Storage   = storage with get, set
        member val At        = at      with get, set
        member val Value:obj = value   with get, set

    type StorageKey = int*string
    let mutable storages = Dictionary<StorageKey, Storage>()
    let mutable storagesById = Dictionary<int, Storage>()
    let mutable logs:Log list = []
    let mutable isLogReader = None
    let mutable lastLogId = -1
    let getStorageKey(s:Storage):StorageKey = s.TagKind, s.Fqdn

    let private initializeCommonOnDemandAsync(systems:DsSystem seq, conn:IDbConnection, tr:IDbTransaction) =
        Log4NetWrapper.logWithTrace <- true
        task {
            let systemStorages: Storage array =
                systems
                |> map (fun s -> s.TagManager)
                |> distinct
                |> Seq.collect (fun tagManager -> tagManager.Storages.Values)
                |> filter (fun s -> s.TagKind <> InnerTag) // 내부변수
                |> distinct
                |> map Storage
                |> toArray
            
            let! dbStorages = conn.QueryAsync<Storage>($"SELECT * FROM [{Tn.Storage}]")
            let dbStorages =
                dbStorages |> map (fun s -> getStorageKey s, s)
                |> Tuple.toDictionary


            let existingStorages, newStorages = 
                systemStorages
                |> Seq.partition (fun s -> dbStorages.ContainsKey(getStorageKey s))

            for s in existingStorages do
                s.Id <- dbStorages[getStorageKey s].Id
                

            for s in newStorages do
                let! id = conn.InsertAndQueryLastRowIdAsync(tr,
                    $"""INSERT INTO [{Tn.Storage}]
                        (name, fqdn, tagKind, dataType)
                        VALUES (@Name, @Fqdn, @TagKind, @DataType)
                        ;
                    """, {|Name=s.Name; Fqdn=s.Fqdn; TagKind=s.TagKind; DataType=s.DataType|})
                s.Id <- id

            storages <-
                (existingStorages @ newStorages)
                |> map (fun s -> getStorageKey s, s)
                |> Tuple.toDictionary
        }

    let ormLog2Log (l:ORMLog) =
        let storage = storagesById[l.StorageId]
        Log(l.Id, storage, l.At, l.Value)// |> tee (fun ll -> ll.Id = l.Id)

    let fetchNewLogs() =
        use conn = createConnection()
        let newLogs = conn.Query<ORMLog>($"SELECT * FROM [{Tn.Log}] WHERE id > {lastLogId} ORDER BY id DESC;")            
        if newLogs.any() then
            let newLogs = newLogs |> map ormLog2Log |> toList
            logs <- newLogs @ logs
            lastLogId <- newLogs |> List.maxBy(fun l -> l.Id) |> fun l -> l.Id
            logDebug $"Feteched {newLogs.length()} new logs.  Total logs = {logs.length()}"


    let mutable readerDisposables = new CompositeDisposable()
    let initializeLogReaderOnDemandAsync(systems:DsSystem seq) =
        isLogReader <- Some true
        dispose readerDisposables
        readerDisposables <- new CompositeDisposable()

        task {
            use conn = createConnection()
            use! tr = conn.BeginTransactionAsync()

            do! initializeCommonOnDemandAsync(systems, conn, tr)

            let! existingLogs = conn.QueryAsync<ORMLog>($"SELECT * FROM [{Tn.Log}]")

            do! tr.CommitAsync()


            storagesById <-
                storages.Values
                |> map (fun s -> s.Id, s)
                |> Tuple.toDictionary

            logs <- existingLogs |> Seq.sortByDescending(fun l -> l.Id) |> map ormLog2Log |> toList
            if logs.any() then
                lastLogId <- logs |> List.maxBy(fun l -> l.Id) |> fun l -> l.Id

            let subs =
                System.Reactive.Linq.Observable.Interval(TimeSpan.FromSeconds(1))
                    .Subscribe(fun counter -> fetchNewLogs())
            readerDisposables.Add(subs)
        }

    let initializeLogWriterOnDemandAsync(systems:DsSystem seq) =
        isLogReader <- Some false
        task {
            use conn = createConnection()
            use! tr = conn.BeginTransactionAsync()
            do! initializeCommonOnDemandAsync(systems, conn, tr)
            do! tr.CommitAsync()
        }

    let private toDecimal (value:obj) =
        match toBool value with
        | Bool b -> (if b then 1 else 0) |> decimal
        | UInt64 d -> decimal d
        | _ -> failwith "ERROR"


    let insertDBLogsAsync(xs:DsLog seq) =
        verify(isLogReader = Some false)
        use conn = createConnection()
        task {
            use! tr = conn.BeginTransactionAsync()
            for x in xs do
                match  x.Storage.Target with
                | Some t ->
                    let key = x.Storage.TagKind, t.QualifiedName
                    let storageId = storages[key].Id
                    let value = toDecimal x.Storage.BoxedValue
                    let! _ = conn.ExecuteAsync(
                        $"""INSERT INTO [{Tn.Log}]
                            (at, storageId, value)
                            VALUES (@At, @StorageId, @Value)
                        """, {| At=x.Time; StorageId=storageId; Value=value |})
                    ()
                | None ->
                    failwith "NOT yet!!"
            do! tr.CommitAsync()
        }

    let insertDBLogAsync(x:DsLog) = insertDBLogsAsync([x])

    //let countFromDBAsync(fqdn:string, tagKind:int, value:bool) =
    //    use conn = createConnection()
    //    conn.QuerySingleAsync<int>(
    //        $"""SELECT COUNT(*) FROM [{Vn.Log}]
    //            WHERE fqdn=@Fqdn AND tagKind=@TagKind AND value=@Value;""", {|Fqdn=fqdn; TagKind=tagKind; Value=value|})

    let countFromDBAsync(fqdns:string seq, tagKinds:int seq, value:bool) =
        use conn = createConnection()
        conn.QuerySingleAsync<int>(
            $"""SELECT COUNT(*) FROM [{Vn.Log}]
                WHERE
                    fqdn IN @Fqdns
                AND tagKind IN @TagKinds
                AND value=@Value;""" 
                , {|Fqdns=fqdns; TagKinds=tagKinds; Value=value|})

                
    // 지정된 조건에 따라 마지막 'Value'를 반환하는 함수
    let GetLastValueFromDBAsync (fqdn: string, tagKind: int) =
        use conn = createConnection()
        conn.QuerySingleOrDefaultAsync<bool>( 
            $"""SELECT Value FROM [{Vn.Log}]
                WHERE fqdn=@Fqdn AND tagKind=@TagKind
                ORDER BY at DESC
                LIMIT 1;""", {|Fqdn=fqdn; TagKind=tagKind|}) 
