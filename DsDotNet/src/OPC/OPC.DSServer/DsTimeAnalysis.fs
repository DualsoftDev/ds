namespace OPC.DSServer

open System
open System.Collections.Generic
open Opc.Ua
open Opc.Ua.Server
open System.Reactive.Subjects
open Engine.Core.Interface
open Engine.Core
open Engine.Core.TagKindModule
open Engine.Runtime
open Dual.Common.Core.FS
open Engine.CodeGenCPU

[<AutoOpen>]
module DsTimeAnalysisMoudle =

    /// 통계 계산 클래스
    type CalcStats() =
        let mutable count = 0
        let mutable mean = 0.0
        let mutable M2 = 0.0
        let mutable activeDuration = 0.0 // startTag → endTag
        let mutable movingDuration = 0.0 // planStart → endTag
        let mutable startTime = DateTime.MinValue
        let mutable planTime = DateTime.MinValue

        /// 시간 기록 시작
        member this.StartTracking(tagType: VertexTag) =
            match tagType with
            | VertexTag.planStart -> planTime <- DateTime.UtcNow
            | VertexTag.startTag -> startTime <- DateTime.UtcNow
            | _ -> ()

        /// 시간 기록 종료 및 지속 시간 계산
        member this.EndTracking() =
            let endTime = DateTime.UtcNow
            if startTime <> DateTime.MinValue then
                activeDuration <- activeDuration + (endTime - startTime).TotalMilliseconds
                startTime <- DateTime.MinValue
            if planTime <> DateTime.MinValue then
                movingDuration <- movingDuration + (endTime - planTime).TotalMilliseconds
                planTime <- DateTime.MinValue

        /// 대기 시간 계산
        member this.WaitingTime = activeDuration - movingDuration

        /// 동작 시간
        member this.ActiveTime = activeDuration
        member this.MovingTime = movingDuration 

        /// 데이터 추가 및 평균/분산 업데이트
        member this.Update(isWork:bool) =
            let duration = if isWork then  activeDuration else movingDuration
            count <- count + 1
            let delta = duration - mean
            mean <- mean + (delta / float count)
            let delta2 = duration - mean
            M2 <- M2 + (delta * delta2)

        /// 평균 값
        member this.Average = mean

        /// 모집단 분산
        member this.GetPopulationVariance() = if count > 0 then M2 / float count else 0.0

        /// 모집단 표준편차
        member this.StandardDeviation = this.GetPopulationVariance() |> Math.Sqrt

        /// 데이터 개수
        member this.Count = count

    /// 태그별 통계 관리
    let statsMap = Dictionary<string, CalcStats>()

    /// 통계 객체 가져오기 (없으면 생성)
    let getOrCreateStats (tagName:string) =
        match statsMap.TryGetValue(tagName) with
        | true, stats -> stats
        | _ ->
            let newStats = CalcStats()
            statsMap.[tagName] <- newStats
            newStats

    /// 통계 업데이트 함수
    let updateStats (vertex:Vertex) (stats:CalcStats) =
        stats.Update(vertex :? Real)
        
        let tagManager = vertex.TagManager :?> VertexTagManager
        tagManager.CalcAverage.BoxedValue <- stats.Average |> float32
        tagManager.CalcStandardDeviation.BoxedValue <- stats.StandardDeviation |> float32
        tagManager.CalcCount.BoxedValue <- stats.Count |> uint
        tagManager.CalcWaitingTime.BoxedValue <- stats.WaitingTime |> float32
        tagManager.CalcActiveTime.BoxedValue <- stats.ActiveTime |> float32
        tagManager.CalcMovingTime.BoxedValue <- stats.ActiveTime |> float32

    /// 태그별 시간 처리 로직
    let processTag tagKind (vertex: Vertex) =
        let stats = getOrCreateStats vertex.QualifiedName
        match tagKind with
        | VertexTag.startTag ->
            stats.StartTracking(VertexTag.startTag)
            debugfn "Tracking started for startTag '%s'." vertex.QualifiedName
        | VertexTag.planStart ->
            stats.StartTracking(VertexTag.planStart)
            debugfn "Tracking started for planTag '%s'." vertex.QualifiedName
        | VertexTag.endTag ->
            stats.EndTracking()
            updateStats vertex stats
            debugfn "Tracking ended for endTag '%s'. Active Time: %f ms, Waiting Time: %f ms"
                vertex.QualifiedName stats.ActiveTime stats.WaitingTime
        | _ -> debugfn "Unhandled VertexTag: %A" tagKind

    /// 이벤트 처리 함수
    let handleCalcTag (stg: IStorage) =
        match stg.Target with
        | Some (:? Vertex as vertex) ->
            match stg.ObjValue with
            | :? bool as isActive when isActive ->
                match Enum.TryParse<VertexTag>(getTagKindName stg.TagKind) with
                | true, tagKind -> 
                    processTag tagKind vertex
                | false, _ -> 
                    debugfn "Invalid TagKind value: %d" stg.TagKind
            | _ -> 
                debugfn "Invalid ObjValue: Expected boolean but got %A" stg.ObjValue
        | _ -> 
            debugfn "Invalid Target: Expected Vertex but got %A" stg.Target
