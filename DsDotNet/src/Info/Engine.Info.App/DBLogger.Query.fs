namespace Engine.Info

open System
open System.Threading.Tasks
open Dapper
open Dual.Common.Core.FS
open System.Data
open Engine.Core

module DBLoggerQueryImpl =
    //type ORMTimeDiff() =
    //    member val At: DateTime = DateTime.MaxValue with get, set
    //    member val PrevAt: DateTime = DateTime.MaxValue with get, set

    let private collectDurationONHelper (loggerInfo:LoggerInfoSet, fqdn: string, tagKind: int) =
        let logs =
            loggerInfo.Logs
            |> filter(fun l -> l.Storage.TagKind = tagKind && l.Storage.Fqdn = fqdn)
            |> List.skipWhile(fun l -> toBool(l.Value) = false)
        let rec inspectLog (logs:Log list) =
            // head 에 최신(最新) 정보가, last 에 최고(最古) 정보 수록
            [   match logs with
                | ([] | _::[]) -> ()
                | off::on::tails when toBool(on.Value) && not <| toBool(off.Value) ->
                    yield (on, off)
                    yield! inspectLog tails
                | on::off::tails when toBool(on.Value) && not <| toBool(off.Value) ->
                    yield! inspectLog (off::tails)

                | on1::on2::tails when toBool(on1.Value) && toBool(on2.Value) ->
                    yield! inspectLog (on2::tails)
                | off::tails when not <| toBool(off.Value) ->
                    yield! inspectLog tails
                | _ -> failwith "ERROR"
            ]
        logs |> inspectLog

    let internal collectDurationsON (loggerInfo:LoggerInfoSet, fqdn: string, tagKind: int) : TimeSpan array =
        collectDurationONHelper (loggerInfo, fqdn, tagKind)
        |> map (fun (prev, curr) -> curr.At - prev.At)
        |> toArray


    let internal getAverageONDurationAsync (loggerInfo:LoggerInfoSet, fqdn: string, tagKind: int) : Task<TimeSpan> =
        task {
            let timeSpans = collectDurationsON (loggerInfo, fqdn, tagKind)

            return
                if timeSpans.any () then
                    timeSpans
                    |> Seq.averageBy (fun ts -> float ts.Ticks)
                    |> int64
                    |> TimeSpan.FromTicks
                else
                    TimeSpan() // 계산된 지속 시간이 없는 경우
        }
