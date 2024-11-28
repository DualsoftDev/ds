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
        let mutable count = 0u
        let mutable mean = 0.0f
        let mutable M2 = 0.0f
        let mutable activeDuration = 0.0 // StatsStart → endTag
        let mutable movingDuration = 0.0 // MovingStart → endTag

        let mutable updateAble = false //drive_state tag가 켜지고 endTag가 살고 다음부터 저장


        member val MovingStart = DateTime.MinValue with get, set
        member val StatsStart = DateTime.MinValue with get, set

        /// 시간 기록 종료 및 지속 시간 계산
        member this.EndTracking() =
            let endTime = DateTime.UtcNow
            if this.StatsStart <> DateTime.MinValue then
                activeDuration <-  (endTime - this.StatsStart).TotalMilliseconds
                this.StatsStart <- DateTime.MinValue
            if this.MovingStart <> DateTime.MinValue then
                movingDuration <-  (endTime - this.MovingStart).TotalMilliseconds
                this.MovingStart <- DateTime.MinValue

        /// 대기 시간 계산
        member this.WaitingDuration  =  activeDuration - movingDuration
        /// 동작 시간
        member this.ActiveDuration  = activeDuration
        member this.MovingDuration  = movingDuration 

        /// 데이터 추가 및 평균/분산 업데이트
        member this.Update(vertex:Vertex) =  
            let tm = vertex.TagManager :?> VertexTagManager

            if updateAble = false then 
                updateAble <- tm.FlowManager.GetFlowTag(FlowTag.drive_state).Value   
            else 
                let duration =  movingDuration |> float32
                count <- count + 1u
                let delta = duration - mean
                mean <- mean + (delta / float32 count)
                let delta2 = duration - mean
                M2 <- M2 + (delta * delta2)
             
                tm.CalcAverage.BoxedValue <- this.Average 
                tm.CalcStandardDeviation.BoxedValue <- this.StandardDeviation 
                tm.CalcCount.BoxedValue <- this.Count 
                tm.CalcWaitingDuration.BoxedValue <- this.WaitingDuration |> uint32
                tm.CalcActiveDuration.BoxedValue <- this.ActiveDuration   |> uint32
                tm.CalcMovingDuration.BoxedValue <- this.MovingDuration   |> uint32

        member this.DriveStateChaged(driveOn:bool) =   
            if not(driveOn) then    //drive_state가 꺼지면 초기화
                updateAble <- false

        /// 평균 값
        member this.Average = mean

        /// 모집단 분산
        member this.GetPopulationVariance() = if count > 0u then (float M2) / (float count) else 0.0

        /// 모집단 표준편차
        member this.StandardDeviation = this.GetPopulationVariance() |> Math.Sqrt |> float32

        /// 데이터 개수
        member this.Count = count

    /// 태그별 통계 관리
    let statsMap = Dictionary<string, CalcStats>()

    /// 통계 객체 가져오기 (없으면 생성)
    let getOrCreateStats (fqdn:string) =
        match statsMap.TryGetValue(fqdn) with
        | true, stats -> stats
        | _ ->
            let newStats = CalcStats()
            statsMap[fqdn] <- newStats
            newStats

    let processFlow (flow: Flow) =
        let driveOn = (flow.TagManager:?> FlowManager).GetFlowTag(FlowTag.drive_state).Value
        flow.GetVerticesOfFlow()
            |> Seq.iter (fun vertex -> 
                let stats = getOrCreateStats (vertex.QualifiedName)
                stats.DriveStateChaged(driveOn)
                
                if vertex :? Real &&  driveOn then
                    stats.StatsStart <- DateTime.UtcNow
                )
       
    /// 태그별 시간 처리 로직
    let processCallTag tagKind (call: Call) =
        let stats = getOrCreateStats call.QualifiedName
        match tagKind with
        | VertexTag.startTag ->
            stats.StatsStart <- DateTime.UtcNow
        | VertexTag.planStart ->
            stats.MovingStart <- DateTime.UtcNow

        | VertexTag.endTag ->
            stats.EndTracking()
            stats.Update(call) 
        | _ -> debugfn "Unhandled VertexTag: %A" tagKind


    /// 태그별 시간 처리 로직
    let processRealTag tagKind (real: Real option) (flow: Flow option) =
        match real, flow with
        | Some real, None ->
            let stats = getOrCreateStats real.QualifiedName
            match tagKind with
            | VertexTag.startTag ->
                stats.MovingStart <- DateTime.UtcNow
            | VertexTag.endTag ->
                stats.EndTracking()
                stats.Update(real) 
            | _ -> debugfn "Unhandled VertexTag: %A" tagKind

        | None, Some flow -> processFlow flow
        | _ -> failWithLog "Invalid arguments: Real: %A, Flow: %A" real flow
       


    /// 이벤트 처리 함수
    let handleCalcTag (stg: IStorage) =
        match stg.Target with
        | Some (:? Vertex as vertex) ->
            match stg.ObjValue with
            | :? bool as isActive when isActive ->
                match Enum.TryParse<VertexTag>(getTagKindName stg.TagKind) with
                | true, tagKind ->
                    if vertex :? Call
                    then
                        processCallTag tagKind (vertex:?> Call)
                    elif vertex :? Real
                    then
                        processRealTag tagKind (Some(vertex:?> Real)) None

                | false, _ -> 
                    failWithLog "Invalid TagKind value: %d" stg.TagKind
            | _ -> ()
        | Some (:? Flow as flow) ->
            match Enum.TryParse<FlowTag>(getTagKindName stg.TagKind) with
            | true, tagKind when tagKind = FlowTag.drive_state -> 
                processFlow flow
            | _ -> 
                failWithLog "Invalid TagKind value: %d" stg.TagKind
        | _ -> 
                failWithLog "Invalid Target: Expected Vertex or Flow but got %A" stg.Target
