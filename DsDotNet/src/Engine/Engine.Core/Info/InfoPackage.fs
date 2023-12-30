namespace Engine.Core

open System.Collections.Generic
open System

[<AutoOpen>]
module InfoPackageModule =
    type IInfoBase =
        inherit INamed
        abstract member Fqdn : string with get, set

    type InfoBase() =
        interface IInfoBase with
            member x.Name with get() = x.Name and set(v) = x.Name <- v
            member x.Fqdn with get() = x.Fqdn and set(v) = x.Fqdn <- v
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
        
    type InfoDevice() = 
        interface IInfoBase with
            member x.Name with get() = x.Name and set(v) = x.Name <- v
            member x.Fqdn with get() = x.Fqdn and set(v) = x.Fqdn <- v
        member val Name = "" with get, set
        member val Fqdn = "" with get, set
        ///동작 횟수
        member val GoingCount = 0 with get, set
        ///고장 횟수
        member val ErrorCount = 0 with get, set
        ///평균 고장 시간
        member val RepairAverage = Nullable<double>() with get, set
        ///총발생한 에러 메시지
        member val ErrorMessages = ResizeArray<string>() with get, set
        static member Create(x:Device) =
            let info = new InfoDevice(Name=x.Name, Fqdn = x.QualifiedName)
            info

    type InfoCall() = 
        inherit InfoBase()
        ///동작 횟수
        member val GoingCount = 0 with get, set
        ///동작 편차
        member val GoingDeviation = 0.0 with get, set
        ///대기 시간
        member val WaitTime = 0.0  with get, set
        member val InfoDevices = HashSet<InfoDevice>()  with get, set
        member val SystemName = "" with get, set
        member val FlowName = "" with get, set
        member val RealName = "" with get, set
        static member Create(x:Call) =
            let info = new InfoCall(Name=x.Name, Fqdn = x.QualifiedName)
            info.SystemName <- x.NameComponents[0]
            info.FlowName <- x.NameComponents[1]
            info.RealName <- x.NameComponents[2]
            info

    type InfoReal() = 
        inherit InfoBase()
        ///동작 횟수
        member val GoingCount = 0 with get, set
        ///대기 시간
        member val WaitTime = 0.0  with get, set
        member val InfoCalls = HashSet<InfoCall>()  with get, set
        member val SystemName = "" with get, set
        member val FlowName = "" with get, set
        static member Create(x:Real) =
            let info = new InfoReal(Name=x.Name, Fqdn = x.QualifiedName)
            info.SystemName <- x.NameComponents[0]
            info.FlowName <- x.NameComponents[1]
            info

    type InfoFlow() = 
        inherit InfoBase()
        ///제품 1개 플로우 처리시간 추후 계산 (알고리즘 필요)
        member val LeadTime = 0.0  with get, set
        member val InfoReals = HashSet<InfoReal>()  with get, set
        member val SystemName = "" with get, set
        static member Create(x:Flow) =
            let info = new InfoFlow(Name=x.Name, Fqdn = x.QualifiedName)
            info.SystemName <- x.NameComponents[0]
            info
    type InfoSystem() = 
        inherit InfoBase()
        ///제품 1개 시스템 처리시간 추후 계산 (알고리즘 필요)
        member val LeadTime = 0.0  with get, set
        member val InfoFlows = HashSet<InfoFlow>()  with get, set
        static member Create(x:DsSystem) =
            let info = new InfoSystem(Name=x.Name, Fqdn = x.QualifiedName)
            info