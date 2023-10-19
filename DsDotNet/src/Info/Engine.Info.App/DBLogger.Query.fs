namespace Engine.Info

open System
open System.Threading.Tasks
open Dapper
open Dual.Common.Core.FS
open System.Data


module DBLoggerQueryImpl =
    type ORMTimeDiff() =
        member val At: DateTime = DateTime.MaxValue with get, set
        member val PrevAt: DateTime = DateTime.MaxValue with get, set

    let private collectDurationONHelperAsync (conn: IDbConnection, fqdn: string, tagKind: int) =
        let query =
            $"""
WITH ChangeEvents AS (
SELECT
    [fqdn],
    [tagKind],
    [at],
    [value],
    LAG([value]) OVER (PARTITION BY [fqdn], [tagKind] ORDER BY [at]) AS prevValue,
    LAG([at]) OVER (PARTITION BY [fqdn], [tagKind] ORDER BY [at]) AS prevAt
FROM
    [{Vn.Log}]
WHERE
    [fqdn] = @Fqdn -- 여기에 원하는 fqdn 값을 입력
    AND [tagKind] = @TagKind -- 여기에 원하는 tagKind 값을 입력
)

SELECT
-- [fqdn],
-- [tagKind],
-- prevValue,
-- [value],
[at],
prevAt
FROM
ChangeEvents
WHERE
[prevValue] = 1 AND [value] = 0
;
"""

        conn.QueryAsync<ORMTimeDiff>(query, {| Fqdn = fqdn; TagKind = tagKind |})

    let collectDurationsONAsync (conn: IDbConnection, fqdn: string, tagKind: int) : Task<TimeSpan seq> =
        task {
            let! (durations: ORMTimeDiff seq) = collectDurationONHelperAsync (conn, fqdn, tagKind)
            return durations |> map (fun d -> d.At - d.PrevAt)
        }


    let getAverageONDurationAsync (conn: IDbConnection, fqdn: string, tagKind: int) : Task<TimeSpan> =
        task {
            let! timeSpans = collectDurationsONAsync (conn, fqdn, tagKind)

            return
                if timeSpans.any () then
                    timeSpans
                    |> Seq.averageBy (fun ts -> float ts.Ticks)
                    |> int64
                    |> TimeSpan.FromTicks
                else
                    TimeSpan() // 계산된 지속 시간이 없는 경우


        }
