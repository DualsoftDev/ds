namespace Engine.Core

open System.Collections.Generic
open System

[<AutoOpen>]
module InfoPackageModule =

    type InfoBase() = 
        member val Name = "" with get, set
        member val Fqdn = "" with get, set
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
        member val Efficiency = Nullable<double>() with get, set
        ///총멈춤 횟수
        member val PauseCount = 0 with get, set

    and InfoSystem() = 
        inherit InfoBase()
        ///제품 1개 시스템 처리시간 추후 계산 (알고리즘 필요)
        member val LeadTime = 0.0  with get, set
        member val InfoFlows = HashSet<InfoFlow>()  with get, set
    
    and InfoFlow() = 
        inherit InfoBase()
        ///제품 1개 플로우 처리시간 추후 계산 (알고리즘 필요)
        member val LeadTime = 0.0  with get, set
        member val InfoReals = HashSet<InfoReal>()  with get, set
        
    and InfoReal() = 
        inherit InfoBase()
        ///동작 횟수
        member val GoingCount = 0 with get, set
        ///대기 시간
        member val WaitTime = 0.0  with get, set
        member val InfoCalls = HashSet<InfoCall>()  with get, set

    and InfoCall() = 
        inherit InfoBase()
        ///동작 횟수
        member val GoingCount = 0 with get, set
        ///동작 편차
        member val GoingDeviation = 0.0 with get, set
        ///대기 시간
        member val WaitTime = 0.0  with get, set
        member val InfoDevices = HashSet<InfoDevice>()  with get, set

    and InfoDevice() = 
        member val Name = "" with get, set
        member val Fqdn = "" with get, set
        ///동작 횟수
        member val GoingCount = 0 with get, set
        ///고장 횟수
        member val ErrorCount = 0 with get, set
        ///평균 고장 시간
        member val RepairAverage = 0.0 with get, set
        ///총발생한 에러 메시지
        member val ErrorMessages = ResizeArray<string>() with get, set

