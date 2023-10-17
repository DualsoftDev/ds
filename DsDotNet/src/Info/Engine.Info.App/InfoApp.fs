namespace Engine.Info.App

open System
open System.Collections.Generic // 사용되지 않는 네임스페이스를 제거합니다.
open Engine.Core
[<AutoOpen>]
module InfoAppM =

    // 로그 목록과 태그 종류, FQDN을 인수로 받는 함수입니다.
    let GetAverage (logs: seq<DsLog>, tagKind: int, fqdn: string) =
        
        // 주어진 조건에 맞는 로그만 필터링합니다.
        let filteredLogs = 
            logs 
            |> Seq.filter (fun log -> 
                    log.Storage.TagKind = tagKind 
                    && log.Storage.Target.IsSome 
                    && log.Storage.Target.Value.QualifiedName = fqdn 
                    && log.Storage.BoxedValue :? bool) // 'bool' 타입인지 확인합니다.
            |> Seq.toList

        // 'true'에서 'false'로 바뀌는 시점 사이의 지속 시간을 계산하는 재귀 함수입니다.
        let rec calculateDurations (logs: DsLog list) (acc: TimeSpan list) =
            match logs with
            | [] | [_] -> List.rev acc // 리스트를 뒤집어 원래의 순서로 복원합니다.
            | first :: second :: rest -> 
                let firstValue = first.Value:?> bool
                let secondValue = second.Value:?> bool

                if firstValue && not secondValue then // true에서 false로 전환되는 경우
                    let duration = second.Time - first.Time // 지속 시간을 계산합니다.
                    calculateDurations rest (duration :: acc) // 지속 시간을 누적 리스트에 추가하고 재귀 호출합니다.
                else
                    calculateDurations (second :: rest) acc // 조건에 맞지 않는 경우, 다음 요소로 넘어갑니다.

        // 필터링된 로그를 사용하여 지속 시간을 계산합니다.
        let durations = calculateDurations filteredLogs []

        // 계산된 지속 시간의 평균을 구합니다.
        match durations with
        | [] -> 0.0 // 계산된 지속 시간이 없는 경우
        | _ -> 
            durations 
            |> List.sumBy (fun duration -> duration.TotalSeconds) // 모든 지속 시간의 합계를 구합니다.
            |> fun sumDuration -> sumDuration / float(List.length durations) // 평균 지속 시간을 반환합니다.
