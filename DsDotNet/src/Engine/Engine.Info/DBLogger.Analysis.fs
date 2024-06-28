namespace Engine.Info

open System.Collections.Generic
open System.Data
open System.Linq
open Dapper
open Dual.Common.Core.FS.CollectionModule
open Dual.Common.Core.FS
open Engine.Core
open DBLoggerORM
open System

[<AutoOpen>]
module internal DBLoggerAnalysisModule =
    /// log 를 fqdn 별로 그룹핑하여 반환한다.
    /// - log 는 시간순 정렬되어 있어야 한다.
    /// - fqdn 의 going ON 부터 finish ON 까지의 log 를 그룹핑하여 반환한다.
    /// - e.g X 의 duration log 를 구한다면
    ///     - 입력 : [ 1; 2; 3; Xs1; 4; 5; 6; Xe1; 7; 8; Xs2; 9; 10; Xe2; ...]
    ///     - 출력 : [ [Xs1; 4; 5; 6; Xe1]; [Xs2; 9; 10; Xe2]; ...]
    let groupDurationsByFqdn (logs: ORMVwLog list) (fqdn:string) : ORMVwLog list list =
        // fqdn 에 해당하는 logging 시작 여부
        let mutable started = false
        // fqdn 시작부터 끝까지의 log 를 중간 저장하기 위한 list
        let mutable building = []
        let folder (acc:ORMVwLog list list) (l:ORMVwLog) =
            let isOn = l.Value.ToString() = "1"
            if l.Fqdn = fqdn && isOn then
                if started then
                    if l.TagKind = int VertexTag.finish then
                        building <- l::building
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
        result


    type LogAnalInfo = {
        DsSystem: DsSystem
        /// 분석 대상 full log list
        Logs:ORMVwLog list
        /// Real 별 [시작 ~ 끝] log list
        PerRealLogs: Dictionary<Real, (ORMVwLog list list)>
    }

    type LogAnalInfo with
        static member Create(system:DsSystem, FList(logs:ORMVwLog list)) : LogAnalInfo =
            let reals = system.Flows |> bind (fun f -> f.Graph.Vertices.OfType<Real>()) |> toArray
            let dic =
                [
                    for r in reals do
                        let realLogs = groupDurationsByFqdn logs r.QualifiedName
                        r, realLogs
                ] |> Tuple.toDictionary

            {
                DsSystem = system
                Logs = logs
                PerRealLogs = dic
            }
        static member Create(system:DsSystem, conn:IDbConnection) : LogAnalInfo =
            let logs = conn.Query<ORMVwLog>($"SELECT * FROM {Vn.Log}") |> toFSharpList
            LogAnalInfo.Create(system, logs)

        member x.PrintStatistics() =
            let getTimeSpan (logs: ORMVwLog list) =
                headAndLast logs |> map (fun (h, t) -> t.At - h.At) |> Option.defaultValue (TimeSpan.FromSeconds 0.0)

            let total = getTimeSpan x.Logs
            tracefn $"Total time duration: {total}"
            tracefn "::: Per Real Logs"

            for KeyValue(r, lss) in x.PerRealLogs do
                tracefn $"::: Total cycles for {r.QualifiedName} = {lss.Length}"
                for (cycle, ls) in lss.Indexed() do
                    let realSpan = getTimeSpan ls
                    tracefn $"  :: Real duration for {r.QualifiedName}, {cycle+1}-th cycle = {realSpan}"

                    for c in r.Graph.Vertices.OfType<Call>() do
                        let callLogs =
                            let fqdn = c.QualifiedName
                            let logs = ls |> filter (fun l -> l.Fqdn = fqdn)
                            groupDurationsByFqdn logs fqdn

                        let spans = callLogs |> map getTimeSpan |> toArray
                        assert (spans.Length = 1)   // 동일 call 이 하나의 real 안에서 여러번 호출되는 경우는 없다고 가정
                        tracefn $"    - {c.Name} = {spans[0]} on cycle {cycle+1}/{lss.Length}"
            ()