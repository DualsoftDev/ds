namespace Engine.Info

open System.Runtime.CompilerServices
open System.Collections.Generic
open System
open Dual.Common.Core.FS
open Engine.Core


[<AutoOpen>]
module InfoPackageModule =

    type InfoBase(name) = 
        member x.Name : string = name
        ///총가동 시간
        member val DriveSpan = 0.0 with get, set
        ///총가동 평균
        member val DriveAverage = 0.0 with get, set
        ///총에러 시간
        member val ErrorSpan = 0.0 with get, set
        ///총에러 평균
        member val ErrorAverage = 0.0 with get, set
        ///총에러 횟수
        member val ErrorCount = 0 with get, set
        ///총발생한 에러 메시지
        member val ErrorMessages = ResizeArray<string>() with get, set
        ///총 효율 %
        member val Efficiency = 0.0 with get, set
        ///총멈춤 횟수
        member val PauseCount = 0 with get, set

    type InfoSystem(name) = 
        inherit InfoBase(name)
        ///제품 1개 시스템 처리시간 추후 계산 (알고리즘 필요)
        member val LeadTime = 0.0  with get, set
    
    type InfoFlow(name) = 
        inherit InfoBase(name)
        ///제품 1개 플로우 처리시간 추후 계산 (알고리즘 필요)
        member val LeadTime = 0.0  with get, set
        
    type InfoReal(name) = 
        inherit InfoBase(name)
        ///동작 횟수
        member val GoingCount = 0 with get, set
        ///대기 시간
        member val WaitTime = 0.0  with get, set

    type InfoCall(name) = 
        inherit InfoBase(name)
        ///동작 횟수
        member val GoingCount = 0 with get, set
        ///동작 편차
        member val GoingDeviation = 0.0 with get, set
        ///대기 시간
        member val WaitTime = 0.0  with get, set

    type InfoDevice(name) = 
        member x.Name : string = name
        ///동작 횟수
        member val GoingCount = 0 with get, set
        ///고장 횟수
        member val ErrorCount = 0 with get, set
        ///평균 고장 시간
        member val RepairAverage = 0.0 with get, set
        ///총발생한 에러 메시지
        member val ErrorMessages = ResizeArray<string>() with get, set

