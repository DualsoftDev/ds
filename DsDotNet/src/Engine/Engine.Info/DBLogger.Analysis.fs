namespace Engine.Info

open Engine.Core
open Dual.Common.Core.FS
open DBLoggerORM

[<AutoOpen>]
module internal DBLoggerAnalysisModule =
    let private groupDurationsByFqdn_FoldBackImpl(logs: ORMVwLog list) (fqdn:string) : ORMVwLog list list =
        let mutable started = false
        let mutable building = []
        let folder (l:ORMVwLog) acc =
            let isOn = unbox l.Value = 1L
            if l.Fqdn = fqdn && isOn then
                if started then
                    if l.TagKind = int VertexTag.finish then
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
        result


    /// log 를 fqdn 별로 그룹핑하여 반환한다.
    /// - log 는 시간순 정렬되어 있어야 한다.
    /// - fqdn 의 going ON 부터 finish ON 까지의 log 를 그룹핑하여 반환한다.
    let groupDurationsByFqdn (logs: ORMVwLog list) (fqdn:string) : ORMVwLog list list =
        let mutable started = false
        let mutable building = []
        let mutable counter = 0
        let folder acc (l:ORMVwLog) =
            counter <- counter + 1
            if (l.TagKind.IsOneOf(int VertexTag.going, int VertexTag.finish)) then
                ()
            let isOn = l.Value.ToString() = "1"
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
                acc
            else
                let res = if building.IsEmpty then acc else (building |> List.rev)::acc
                building <- []
                res

        let result = List.fold folder [] logs |> List.rev
        tracefn $"LOOP: {counter}"
        result
