namespace Engine.Info

open System
open Dual.Common.Core.FS
open Engine.Core

[<AutoOpen>]
module internal DBLoggerQueryImpl =
    //[<Obsolete>]
    //let collectDurationONLogPairs (logSet:LogSet, fqdn: string, tagKind: int) : (Log*Log) array=
    //    /// head 에 최신(最新) 정보가, last 에 최고(最古) 정보 수록
    //    let logs =
    //        logSet.Logs
    //        |> filter(fun l -> l.Storage.TagKind = tagKind && l.Storage.Fqdn = fqdn)
    //        |> List.skipWhile(fun l -> toBool(l.Value) = true)  // 최신에 켜져서 가동 중인 frame 무시

    //    let rec inspectLog (logs:Log list) =
    //        seq {
    //            match logs with
    //            | ([] | _::[]) -> ()
    //            | off::on::tails when toBool(on.Value) && not <| toBool(off.Value) ->
    //                yield (on, off)
    //                yield! inspectLog tails
    //            | on::off::tails when toBool(on.Value) && not <| toBool(off.Value) ->
    //                yield! inspectLog (off::tails)

    //            | on1::on2::tails when toBool(on1.Value) && toBool(on2.Value) ->
    //                yield! inspectLog (on2::tails)
    //            | off::tails when not <| toBool(off.Value) ->
    //                yield! inspectLog tails
    //            | _ -> failwith "ERROR"
    //        }
    //    logs |> inspectLog |> Seq.rev |> toArray

    //let collectONDurations (logSet:LogSet, fqdn: string, tagKind: int) : TimeSpan array =
    //    collectDurationONLogPairs (logSet, fqdn, tagKind)
    //    |> map (fun (prev, curr) -> curr.At - prev.At)

    let sum (logSet:LogSet, fqdn: string, tagKind: int) : double =
        let summary = logSet.GetSummary(tagKind, fqdn)
        summary.Sum

    let average (logSet:LogSet, fqdn: string, tagKind: int) : double =
        let summary = logSet.GetSummary(tagKind, fqdn)
        if summary.Count > 0 then
            summary.Sum / (double summary.Count)
        else
            9


    type Summary with
        // logs: id 순
        member x.Build(FList(logs:Log list)) =
            let mutable count = 0
            let mutable sum = 0.0
            let isOn (log:Log) = toBool(log.Value)
            let isOff = isOn >> not

            let rec inspectLog (logs:Log list) =
                match logs with
                | ([] | _::[]) ->
                    ()

                | log1::log2::tails ->
                    let b1, b2 = isOn(log1), isOn(log2)
                    match b1, b2 with
                    | true, false ->
                        count <- count + 1
                        let duration = (log2.At - log1.At).TotalSeconds
                        assert (duration >= 0)
                        sum <- sum + duration                    
                        inspectLog tails
                    | _ when b1 = b2 ->
                        failwithlogf $"ERROR.  duplicated consecutive values detected."
                    | _ ->
                        failwithlogf $"ERROR.  Expect (rising, falling)."
            
            logs |> inspectLog

            x.Count <- count
            x.Sum <- sum
            x.LastLog <- logs.TryLast()
            ()

        member x.BuildIncremental(FList(newLogs:Log list)) =
            let lastLog = newLogs |> Seq.tryLast
            match x.LastLog, lastLog with
            | Some l, Some last ->
                if l.Value = last.Value then
                    failwithlogf $"ERROR.  duplicated consecutive values detected."
            | _  -> ()

            x.LastLog <- lastLog

    type LogSet with
        member x.BuildIncremental(newLogs:Log seq) =
            let logs = newLogs
            let groups =
                logs |> Seq.groupBy(fun l -> getStorageKey x.StoragesById[l.StorageId] )
            for (key, group) in groups do
                x.Summaries[key].BuildIncremental(group)

            if logs.any() then
                x.LastLog <- logs |> Seq.tryLast
            ()
