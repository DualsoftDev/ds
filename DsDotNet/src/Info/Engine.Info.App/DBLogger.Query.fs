namespace Engine.Info

open System
open Dual.Common.Core.FS
open Engine.Core

[<AutoOpen>]
module internal DBLoggerQueryImpl =

    let sum (logSet:LogSet, fqdn: string, tagKind: int) : double =
        let summary = logSet.GetSummary(tagKind, fqdn)
        summary.Sum

    let average (logSet:LogSet, fqdn: string, tagKind: int) : double =
        let summary = logSet.GetSummary(tagKind, fqdn)
        if summary.Count > 0 then
            summary.Sum / (double summary.Count)
        else
            0


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
