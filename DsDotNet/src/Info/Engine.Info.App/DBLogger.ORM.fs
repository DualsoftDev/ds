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
module DBLoggerModule =
    // database table names
    module Tn =
        let Storage = "storage"
        let Log = "log"
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
);

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
    let private createConnection() =
        new SqliteConnection(connectionString) |> tee (fun conn -> conn.Open())

    let createLoggerDBSchema() =
        use conn = createConnection()
        if not <| conn.IsTableExistsAsync(Tn.Log).Result then
            conn.Execute(sqlCreateSchema, null) |> ignore

    let InitializeOnDemandAsync(systems:DsSystem seq) =
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

    let InsertDBLogAsync(x:DsLog) =
        use conn = createConnection()
        task {
            match  x.Storage.Target with
            | Some t ->
                let fqdn = t.QualifiedName
                let! storageId =
                    conn.QuerySingleOrDefaultAsync<int>(
                        $"""SELECT id FROM [{Tn.Storage}]
                            WHERE fqdn=@Fqdn AND tagKind=@TagKind;""", {|Fqdn=fqdn; TagKind=x.Storage.TagKind|})
                let value = toDecimal x.Value
                let! _ = conn.ExecuteAsync(
                    $"""INSERT INTO [{Tn.Log}]
                        (at, storageId, value)
                        VALUES (@At, @StorageId, @Value)
                    """, {| At=x.Time; StorageId=storageId; Value=value |})
                ()
            | None ->
                failwith "NOT yet!!"
        }

