namespace Engine.Info

open Engine.Core
open DBLoggerORM

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
        let folder acc (l:ORMVwLog) =
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
