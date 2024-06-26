namespace Engine.Info

open System
open System.Configuration
open System.Collections.Concurrent
open Dapper
open Engine.Core
open Microsoft.Data.Sqlite
open Dual.Common.Core.FS
open Dual.Common.Db
open System.Collections.Generic
open System.Data
open System.Reactive.Disposables
open System.Threading.Tasks
open DBLoggerORM

[<AutoOpen>]
module internal DBLoggerAnalysisModule =
    let groupDurationsByFqdn (logs: ORMVwLog list) (fqdn:string) =
        let mutable started = false
        let mutable building = []
        let mutable counter = 0
        let folder (l:ORMVwLog) acc =
            counter <- counter + 1
            if (l.TagKind.IsOneOf(int VertexTag.going, int VertexTag.finish)) then
                ()
            let isOn = unbox l.Value = 1L
            if l.Fqdn = fqdn && isOn then
                if started then
                    //if l.TagKind = int VertexTag.finish then
                    if l.TagKind = int VertexTag.homing then
                        started <- false
                else
                    if l.TagKind = int VertexTag.going then
                        started <- true
            if started then
                building <- l::building
                match acc with
                | [] -> [building]
                | _ -> acc
            else
                let res = if building.IsEmpty then acc else building::acc
                building <- []
                res

        let result = List.foldBack folder logs []
        tracefn $"LOOP: {counter}"
        result


        //for l in logs do
        //    if l.Fqdn = fqdn then
        //        if l.TagKind = VertexTag.going && l.Value = 1 then
        //            started <- true
        //        elif l.TagKind = VertexTag.coming && l.Value = 1 && started then
        //        yield l
        //    else
        //        yield! Array.empty<ORMVwLog> // yield! Array.empty<ORMVwLog> is equivalent to yield! []
        //let spitLogs logs =
        //logs
        //|> Seq.groupBy (fun log -> log.LogTime.Date)
        //|> Seq.map (fun (date, logs) -> date, logs |> Seq.length)
        //|> Seq.toArray


