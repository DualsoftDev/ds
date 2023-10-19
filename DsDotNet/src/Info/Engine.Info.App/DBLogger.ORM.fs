namespace Engine.Info

open System
open System.Configuration;
open Dapper
open Engine.Core
open Microsoft.Data.Sqlite
open Dual.Common.Core.FS
open Dual.Common.Db
open System.Collections.Generic


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
    , CONSTRAINT uniq_fqdn UNIQUE (fqdn, tagKind)
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

    let initializeOnDemandAsync(systems:DsSystem seq) =
        task {
            use conn = createConnection()
            use! tr = conn.BeginTransactionAsync()
            let! existingFqdns = conn.QueryAsync<string>($"SELECT fqdn FROM [{Tn.Storage}]")
            let existingFqdns = HashSet<string>(existingFqdns)
                                
            let storages: IStorage seq =
                systems
                |> map (fun s -> s.TagManager)
                |> distinct
                |> Seq.collect (fun tagManager -> tagManager.Storages.Values)
                |> distinct
            
            let newStorages =
                storages
                |> filter (fun s -> s.TagKind <> InnerTag) // 내부변수
                |> filter (fun s -> not <| existingFqdns.Contains(s.Target.Value.QualifiedName))
                |> Array.ofSeq

            for s in newStorages do
                let fqdn = s.Target.Value.QualifiedName
                let dataType = s.DataType.Name
                let! _ = conn.ExecuteAsync(
                    $"""INSERT INTO [{Tn.Storage}]
                        (name, fqdn, tagKind, dataType)
                        VALUES (@Name, @Fqdn, @TagKind, @DataType)
                        ;
                    """, {|Name=s.Name; Fqdn=fqdn; TagKind=s.TagKind; DataType=dataType|})
                ()
                
            do! tr.CommitAsync()
            ()
        }

    let private toDecimal (value:obj) =
        match toBool value with
        | Bool b -> (if b then 1 else 0) |> decimal
        | UInt64 d -> decimal d
        | _ -> failwith "ERROR"

    let insertDBLogAsync(x:DsLog) =
        use conn = createConnection()
        task {
            match  x.Storage.Target with
            | Some t ->
                let fqdn = t.QualifiedName
                let! storageId =
                    conn.QuerySingleOrDefaultAsync<int>(
                        $"""SELECT id FROM [{Tn.Storage}]
                            WHERE fqdn=@Fqdn AND tagKind=@TagKind;""", {|Fqdn=fqdn; TagKind=x.Storage.TagKind|})
                let value = toDecimal x.Storage.BoxedValue
                let! _ = conn.ExecuteAsync(
                    $"""INSERT INTO [{Tn.Log}]
                        (at, storageId, value)
                        VALUES (@At, @StorageId, @Value)
                    """, {| At=x.Time; StorageId=storageId; Value=value |})
                ()
            | None ->
                failwith "NOT yet!!"
        }

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
