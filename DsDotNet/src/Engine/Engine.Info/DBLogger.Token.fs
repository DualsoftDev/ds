namespace Engine.Info

open System.Runtime.CompilerServices
open System.Threading.Tasks
open Microsoft.Data.Sqlite
open Dual.Common.Core.FS
open System.Data
open Dapper
open Dual.Common.Db
open System.Reactive.Subjects
open System.Threading
open Dual.Common.Base.FS
open Newtonsoft.Json


type DBLoggerToken() =
    static member CollectLogsWithTokenAsync(conn:IDbConnection, token:uint) =
        conn.QueryAsync<ORMLog>($"SELECT * FROM [{Tn.Log}] WHERE token = {token} ORDER BY token;")

[<AutoOpen>]
module DBLoggerToken =
    //let collectLogAsync (conn:IDbConnection) (token:uint): ORMLog seq =
    //    ()
         //               let! logs = conn.QueryAsync<ORMLog>($"SELECT * FROM [{Tn.Log}] WHERE id > {lastId};") |> Async.AwaitTask
         ()


