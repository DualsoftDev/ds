namespace Engine.Info

open System.Data
open Dapper


type DBLoggerToken() =
    static member CollectLogsWithTokenAsync(conn:IDbConnection, token:uint) =
        conn.QueryAsync<ORMLog>($"SELECT * FROM [{Tn.Log}] WHERE token = {token} ORDER BY token;")

[<AutoOpen>]
module DBLoggerToken =
    //let collectLogAsync (conn:IDbConnection) (token:uint): ORMLog seq =
    //    ()
         //               let! logs = conn.QueryAsync<ORMLog>($"SELECT * FROM [{Tn.Log}] WHERE id > {lastId};") |> Async.AwaitTask
         ()


