namespace Engine.Info

open System
open System.Data
open Dapper
open System.Data.SqlClient

[<AutoOpen>]
module DBCommon =

    type IDbConnection with
        member this.TryQuerySingle<'T>(sql: string, param: obj, transaction: IDbTransaction) : 'T option =
            try
                let result = this.QuerySingleOrDefault<'T>(sql, param, transaction)
                if box result = null then None else Some result
            with
            | :? InvalidOperationException ->
                // Handle the case where the query returned no results
                None
            | ex ->
                // Optionally log or handle other exceptions
                raise ex

    type DateTime with
        member x.TruncateMilliseconds() =
            new DateTime(x.Year, x.Month, x.Day, x.Hour, x.Minute, x.Second)