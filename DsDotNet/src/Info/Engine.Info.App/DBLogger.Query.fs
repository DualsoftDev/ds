namespace Engine.Info

open System
open Dual.Common.Core.FS
open Engine.Core

module internal DBLoggerQueryImpl =

    let collectDurationONLogPairs (logSet:LogSet, fqdn: string, tagKind: int) : (Log*Log) array=
        /// head 에 최신(最新) 정보가, last 에 최고(最古) 정보 수록
        let logs =
            logSet.Logs
            |> filter(fun l -> l.Storage.TagKind = tagKind && l.Storage.Fqdn = fqdn)
            |> List.skipWhile(fun l -> toBool(l.Value) = true)  // 최신에 켜져서 가동 중인 frame 무시

        let rec inspectLog (logs:Log list) =
            seq {
                match logs with
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
            }
        logs |> inspectLog |> Seq.rev |> toArray

    let collectONDurations (logSet:LogSet, fqdn: string, tagKind: int) : TimeSpan array =
        collectDurationONLogPairs (logSet, fqdn, tagKind)
        |> map (fun (prev, curr) -> curr.At - prev.At)


    let getAverageONDuration (logSet:LogSet, fqdn: string, tagKind: int) : TimeSpan option =
        let timeSpans = collectONDurations (logSet, fqdn, tagKind)

        if timeSpans.any () then
            timeSpans
            |> Seq.averageBy (fun ts -> float ts.Ticks)
            |> int64
            |> TimeSpan.FromTicks
            |> Some
        else
            None // 계산된 지속 시간이 없는 경우
