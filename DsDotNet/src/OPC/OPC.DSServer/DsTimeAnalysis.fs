namespace OPC.DSServer

open System
open System.Collections.Generic
open System.IO
open Opc.Ua
open Opc.Ua.Server
open System.Reactive.Subjects
open Engine.Core.Interface
open Engine.Core
open Engine.Core.TagKindModule
open Engine.Runtime
open Dual.Common.Core.FS
open Engine.CodeGenCPU
open Newtonsoft.Json
open System.Configuration
open System.Collections.Concurrent

[<AutoOpen>]
module DsTimeAnalysisMoudle =

    /// 통계 계산 클래스
    type CalcStats() =
        let mutable count = 0u
        let mutable mean = 0.0f
        let mutable M2 = 0.0f
        let mutable activeDuration = 0u // StatsStart → finishTag
        let mutable movingDuration = 0u // MovingStart → finishTag
        
        let mutable activeLastLog = 0u 
        let mutable movingLastLog = 0u 


        let mutable statsStart = DateTime.MinValue 
        let mutable movingStart = DateTime.MinValue 
        let mutable updateAble = false //drive_state tag가 켜지고 finishTag가 살고 다음부터 저장


        /// 모집단 분산
        let getPopulationVariance() = if count > 0u then (float M2) / (float count) else 0.0
        /// 모집단 표준편차
        let getStandardDeviation() = getPopulationVariance() |> Math.Sqrt |> float32

        let resetStat(vertex:Vertex) =  
            let tm = vertex.TagManager :?> VertexTagManager
            tm.CalcWaitingDuration.BoxedValue <- 0u
            tm.CalcActiveDuration.BoxedValue <-  0u
            tm.CalcMovingDuration.BoxedValue <-  0u
            tm.CalcStatWorkFinish.BoxedValue <- false  

        /// 데이터 추가 및 평균/분산 업데이트
        let updateStat(vertex:Vertex) =  
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
             
                tm.CalcAverage.BoxedValue <- mean
                tm.CalcStandardDeviation.BoxedValue <- getStandardDeviation() 
                tm.CalcCount.BoxedValue <- count

                tm.CalcWaitingDuration.BoxedValue <- activeDuration - movingDuration
                tm.CalcActiveDuration.BoxedValue <- activeDuration  
                tm.CalcMovingDuration.BoxedValue <- movingDuration  

                activeLastLog <- activeDuration
                movingLastLog <- movingDuration
        //member x.StatsStart = statsStart
        //member val MovingStart = DateTime.MinValue with get, set
    
        member this.StartTracking(vertex:Vertex, startTime:DateTime) =  
            statsStart <- startTime
            let tm = vertex.TagManager :?> VertexTagManager
            tm.CalcActiveStartTime.BoxedValue <- 
                TimeZoneInfo.ConvertTime(statsStart, TimeZoneInfo.Utc, TimeZoneInfo.Local)
                            .ToString("yyyy-MM-dd HH:mm:ss.fff");
        /// Moving 기록 시작
        member this.StartMoving() =  
            movingStart <- DateTime.UtcNow
        
        /// 시간 기록 종료 및 지속 시간 계산
        member this.EndTracking(vertex:Vertex) =
            let tm = vertex.TagManager :?> VertexTagManager
            let endTime = DateTime.UtcNow
            if statsStart <> DateTime.MinValue then
                activeDuration <-  (endTime - statsStart).TotalMilliseconds |> uint32
            if movingStart <> DateTime.MinValue then
                movingDuration <-  (endTime - movingStart).TotalMilliseconds |> uint32
            //else 
            //    failwithf $"{vertex.QualifiedName} 신호놓침 planStart"

            resetStat vertex  //opc rising 위해서 값 초기화
            updateStat vertex  
            
            tm.CalcStatWorkFinish.BoxedValue <- true //rising 처리

            statsStart <- DateTime.MinValue
            movingStart <- DateTime.MinValue
            activeDuration <- 0u
            movingDuration <- 0u


      
        /// 대기 시간 계산
        member this.WaitingDuration  =  activeDuration - movingDuration
        /// 동작 시간
        member this.ActiveLastLog  = activeLastLog
        member this.MovingLastLog  = movingLastLog


        member this.DriveStateChaged(driveOn:bool) =   
            if not(driveOn) then    //drive_state가 꺼지면 초기화
                updateAble <- false

        member x.Count with get () = count and set v = count <- v
        member x.Mean with get () = mean and set v = mean <- v
        member x.MeanTemp with get () = M2 and set v = M2 <- v

        member x.ActiveDuration with get () = activeDuration and set v = activeDuration <- v
        member x.MovingDuration with get () = movingDuration and set v = movingDuration <- v

        /// 모집단 표준편차
        member this.StandardDeviation = getStandardDeviation() 


    /// 태그별 통계 관리
    let statsMap = ConcurrentDictionary<string, CalcStats>()
    let getStatsJson() = 
        statsMap
        |> Seq.filter(fun kvp -> kvp.Key.IsNonNull()) //null이 아닌 것만 필터링 todo null 아예 안나게 처리필요
        |> Seq.map(fun kvp -> 
            let statJson =  
                {   
                    Count = kvp.Value.Count;
                    Mean = kvp.Value.Mean; 
                    MeanTemp = kvp.Value.MeanTemp;
                    ActiveTime = kvp.Value.ActiveLastLog;
                    MovingTime = kvp.Value.MovingLastLog;
                }
            kvp.Key, statJson) 
        |> dict   
        
    let getCalcStats(statsDto:StatsDto) = 
        let calcStats = CalcStats()
        calcStats.Count <- statsDto.Count   
        calcStats.Mean <- statsDto.Mean
        calcStats.MeanTemp <- statsDto.MeanTemp
        calcStats.ActiveDuration <- statsDto.ActiveTime
        calcStats.MovingDuration <- statsDto.MovingTime
        calcStats

    /// 통계 객체 가져오기 (없으면 생성)
    let getOrCreateStats (fqdn:string) =
        match statsMap.TryGetValue(fqdn) with
        | true, stats -> stats
        | _ ->
            let newStats = CalcStats()
            statsMap[fqdn] <- newStats
            newStats

    let initUpdateStat (dsSys:DsSystem) =
        dsSys.Flows.Iter(fun flow -> 
            flow.GetVerticesOfFlow()
            |> Seq.iter (fun vertex -> 
                let stats = getOrCreateStats (vertex.QualifiedName)
                let vm = vertex.TagManager :?> VertexTagManager  
                vm.CalcAverage.BoxedValue <- stats.Mean 
                vm.CalcStandardDeviation.BoxedValue <-stats.StandardDeviation 
                vm.CalcActiveDuration.BoxedValue <-stats.ActiveDuration 
                vm.CalcMovingDuration.BoxedValue <-stats.MovingDuration 
                vm.CalcCount.BoxedValue <-stats.Count 
                )
            )

    let processFlow (flow: Flow) =
        let driveOn = (flow.TagManager:?> FlowManager).GetFlowTag(FlowTag.drive_state).Value
        flow.GetVerticesOfFlow()
            |> Seq.iter (fun vertex -> 
                let stats = getOrCreateStats (vertex.QualifiedName)
                stats.DriveStateChaged(driveOn)
                
                if vertex :? Real &&  driveOn then
                    stats.StartTracking(vertex, DateTime.UtcNow)
                )
       
    /// 태그별 시간 처리 로직
    let processCallTag tagKind (call: Call) =
        let stats = getOrCreateStats call.QualifiedName
        match tagKind with
        | VertexTag.startTag ->
            stats.StartTracking(call, DateTime.UtcNow) 
        | VertexTag.going    ->
            stats.StartMoving() 

        | VertexTag.calcStatActionFinish ->
            stats.EndTracking(call)
        | _ -> debugfn "Unhandled VertexTag: %A" tagKind


    /// 태그별 시간 처리 로직
    let processRealTag tagKind  (real: Real option) (flow: Flow option) =
        match real, flow with
        | Some real, None ->
            let stats = getOrCreateStats real.QualifiedName
            match tagKind with
            | VertexTag.startTag ->
                stats.StartMoving()
            | VertexTag.endTag->
                stats.EndTracking(real)    //work는 종료하고 바로 시작(실제Going을 Moving으로 처리)
            | VertexTag.finish ->
                let startDelay = 100  
                async {
                    let sTime = DateTime.UtcNow
                    do! Async.Sleep(startDelay) // 100ms 지연  // work는 종료하고 바로 시작해서 다음 StartStat 시간이 처리됨 방지
                    stats.StartTracking(real, sTime)//(*DateTime.UtcNow.AddMilliseconds(-startDelay)*))
                } |> Async.Start // 비동기로 처리
            | _ -> debugfn "Unhandled VertexTag: %A" tagKind

        | None, Some flow -> processFlow flow
        | _ -> failWithLog "Invalid arguments: Real: %A, Flow: %A" real flow
       


    /// 이벤트 처리 함수
    let handleCalcTag (stg: IStorage) =
        match stg.Target with
        | Some (:? Vertex as vertex) ->
            match stg.ObjValue with
            | :? bool as isActive  when isActive ->
                match Enum.TryParse<VertexTag>(getTagKindName stg.TagKind) with
                | true, tagKind ->
                    if vertex :? Call
                    then
                        processCallTag tagKind (vertex:?> Call)
                    elif vertex :? Real
                    then
                        processRealTag tagKind  (Some(vertex:?> Real)) None

                | false, _ -> 
                    failWithLog "Invalid TagKind value: %d" stg.TagKind
            | _ -> ()
        | Some (:? Flow as flow) ->
            match Enum.TryParse<FlowTag>(getTagKindName stg.TagKind) with
            | true, tagKind when tagKind = FlowTag.drive_state -> 
                processFlow flow
            | _ -> 
                ()
        | _ -> 
                failWithLog "Invalid Target: Expected Vertex or Flow but got %A" stg.Target

