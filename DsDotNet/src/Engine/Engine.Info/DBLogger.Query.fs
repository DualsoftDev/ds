namespace Engine.Info

open System
open Dual.Common.Core.FS
open Engine.Core
open System.Collections.Generic

[<AutoOpen>]
module internal DBLoggerQueryImpl =

    let sum     (logSet: LogSet, fqdn: string, tagKind: int) : double = logSet.GetSummary(tagKind, fqdn).Sum
    let average (logSet: LogSet, fqdn: string, tagKind: int) : double = logSet.GetSummary(tagKind, fqdn).Average

    [<Obsolete("failwithlogf 로 대체 되어야 함")>]
    let pseudoFail msg =
        failwithlogf msg
        //logWarn msg
        ()

    let isOn (log: Log) = toBool (log.Value)
    let isOff = isOn >> not

    type Summary with
        // logs: id 순
        member x.Build(FList(logs: Log list), lastLogs:Dictionary<ORMStorage, Log>) =
            let rec updateSummary (logs: Log list) =
                match logs with
                | ([] | [ _ ]) -> ()

                | log1 :: log2 :: tails ->
                    let b1, b2 = isOn (log1), isOn (log2)

                    match b1, b2 with
                    | true, false ->
                        let duration = (log2.At - log1.At).TotalSeconds
                        x.Durations.Add duration
                        updateSummary tails
                    //| _ when b1 = b2 -> failwithlogf $"ERROR.  duplicated consecutive values detected. ({log1.Storage.Name}:{b1}, {log2.Storage.Name}:{b2})"
                    | _ when b1 = b2 -> ()  // todo: replace this line with above line
                    | _ -> logError $"ERROR.  Expect ({log1.Storage.Name}:rising, {log2.Storage.Name}:falling)."

            logs |> updateSummary

            logs
            |> groupBy(fun l -> l.Storage)
            |> map (fun (key, group) -> group |> last)
            |> iter( fun l -> lastLogs[l.Storage] <- l)

            ()

        member x.BuildIncremental(FList(newLogs: Log list), lastLogs:Dictionary<ORMStorage, Log>) =
            let helper (current: Log) =
                match lastLogs.TryFindValue(current.Storage) with
                | Some last ->
                    if current.Id >= 0 && isOn (last) = isOn (current) then
                        logWarn  $"Warning: Duplicated consecutive values detected for log id = {current.Id}, prev log id = {last.Id}."

                    if isOff (current) then
                        (current.At - last.At).TotalSeconds |> x.Durations.Add
                | None ->
                    if isOff (current) then
                        logWarn $"Warning: Invalid value starts: OFF(false)."

                lastLogs[current.Storage] <- current

            newLogs |> iter helper

    type LogSet with

        member x.BuildIncremental(newLogs: Log seq) =
            let logs = newLogs

            let groups =
                logs |> Seq.groupBy (fun l -> getStorageKey x.StoragesById[l.StorageId])

            for (key, group) in groups do
                x.Summaries[key].BuildIncremental(group, x.LastLogs)

            x.TheLastLog <- logs |> Seq.tryLast

            ()
