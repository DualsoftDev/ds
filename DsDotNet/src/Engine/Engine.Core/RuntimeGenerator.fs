namespace Engine.Core

open System.Reactive.Subjects
open Dual.Common.Core.FS
open System.Collections.Generic
open Dual.Common.Base.FS.Functions

[<AutoOpen>]
module RuntimeGeneratorModule =

    type RuntimePackage =
        | Simulation
        | Control
        | Monitoring
        | VirtualPlant
        | VirtualLogic
    with
        member x.IsVirtualMode() =
            match x with
            | Simulation
            | VirtualPlant
            | VirtualLogic -> true
            | _ -> false

        member x.IsControlMode() =
            match x with
            | Control -> true
            | _ -> false
        ///PLC, PC 값을 읽기만 함
        member x.IsMonitorOnlyMode() =
            match x with
            | Monitoring
            | Simulation
            | VirtualLogic -> true
            | _ -> false

    let ToRuntimePackage s =
        match s with
        | "Simulation" -> Simulation
        | "Control" -> Control
        | "Monitoring" -> Monitoring
        | "VirtualPlant" -> VirtualPlant
        | "VirtualLogic" -> VirtualLogic
        | _ -> Simulation

    //제어 HW CPU 기기 타입
    type PlatformTarget =
        | WINDOWS
        | XGI
        | XGK
        static member ofString(str:string) = DU.fromString<PlatformTarget> str |?? (fun () -> failwith "ERROR")

        member x.Stringify() = x.ToString()
        member x.IsPLC = x <> WINDOWS
        member x.TryGetPlcType() =
            match x with
            | WINDOWS -> None
            | _ -> Some x


    //제어 Driver IO 기기 타입
    type HwDriveTarget =
        | LS_XGI_IO
        | LS_XGK_IO
        | MELSEC_IO
        | OPC_IO

    //제어 Driver IO 기기 타입
    type ExternalApi =
        | OPC
        | REDIS

    //HW CPU,  Driver IO, Slot 정보 조합
    type HwTarget(platformTarget:PlatformTarget, hwDriveTarget:HwDriveTarget, slots:SlotDataType[]) =
        member x.Platform = platformTarget
        member x.HwDrive = hwDriveTarget
        member x.Slots = slots

    type RuntimeMotionMode =
        | MotionAsync
        | MotionSync

    type TimeSimutionMode =
        | TimeNone
        | TimeX0_1
        | TimeX0_5
        | TimeX1
        | TimeX2
        | TimeX4
        | TimeX8
        | TimeX16
        | TimeX100

    let InitStartMemory = 1000
    let BufferAlramSize = 10000
    let XGKAnalogOffsetByte = 96
    let XGKAnalogOutOffsetByte = 96


    let ExternalTempMemory =  "M"
    let ExternalTempIECMemory =  "%MX"
    let ExternalXGIAddressON = "%FX153"
    let ExternalXGIAddressOFF = "%FX154"
    let ExternalXGKAddressON = "F00099"
    let ExternalXGKAddressOFF = "F0009A"
    let ExternalTempNoIECMemory =  "M"


    let HMITempMemory =  "%HX99"  //iec xgk 구분안함
    let HMITempManualAction =  "%HX0"  //iec xgk 구분안함


    let getExternalTempMemory (target:HwTarget, index:int) =
        match target.Platform with
        | XGI-> ExternalTempIECMemory+index.ToString()
        | XGK-> ExternalTempNoIECMemory+index.ToString("00000")
        | WINDOWS-> ExternalTempMemory+($"{index/8}.{index%8}")

    type ModelConfig = {
        DsFilePath: string
        mutable HwIP: string
        HwOPC : string
        HwPath: string
        ExternalApi: ExternalApi
        RuntimePackage: RuntimePackage
        PlatformTarget: PlatformTarget
        HwDriver: HwDriveTarget
        TimeSimutionMode : TimeSimutionMode
        TimeoutCall : uint32
    }
    with    
        member x.ToMessage() = 
            let baseMsg = $"DsFilePath: {x.DsFilePath}\r\nRuntimePackage: {x.RuntimePackage}"
            let hwIOInfo = 
                match x.HwDriver with
                | LS_XGI_IO -> x.HwIP
                | LS_XGK_IO -> x.HwIP
                | MELSEC_IO -> x.HwPath
                | OPC_IO -> x.HwOPC
            let hwIOPlatformTarget = 
                match x.PlatformTarget with
                | WINDOWS -> hwIOInfo
                | XGI -> "LS_XGI_IO"
                | XGK -> "LS_XGK_IO"

            match x.RuntimePackage with
            | Simulation ->
                baseMsg + $"\r\nTimeSimutionMode: {x.TimeSimutionMode}"
            | Control -> 
                baseMsg + $"\r\nPlatformTarget: {x.PlatformTarget}
                \r\nHwDriver: {x.HwDriver} ({hwIOPlatformTarget}) 
                \r\nTimeoutCall: {x.TimeoutCall}"
            | Monitoring 
            | VirtualPlant -> 
                baseMsg + $"\r\nHwDriver: {x.HwDriver}({hwIOInfo})
                \r\nTimeoutCall: {x.TimeoutCall}"
            | VirtualLogic -> 
                baseMsg + $"\r\nTimeSimutionMode: {x.TimeSimutionMode}\r\nExternalApi: {x.ExternalApi}
                \r\nTimeoutCall: {x.TimeoutCall}"


    let createDefaultModelConfig() =
        { 
            DsFilePath = ""
            HwIP = "127.0.0.1"
            HwOPC = "opc.tcp://127.0.0.1:2747"
            HwPath = "0"
            ExternalApi = ExternalApi.OPC
            RuntimePackage = Simulation //unit test를 위해 Simulation으로 설정
            PlatformTarget = WINDOWS
            HwDriver = HwDriveTarget.LS_XGK_IO
            TimeSimutionMode = TimeX1
            TimeoutCall = 15000u
        }
    let createDefaultModelConfigWithHwDriver(hwDriver: HwDriveTarget) =
        { createDefaultModelConfig() with HwDriver = hwDriver }
    let createModelConfigWithSimMode(config: ModelConfig, package:RuntimePackage) =
        { config with RuntimePackage = package }

    let createModelConfig(path:string,
            hwIP:string, 
            hwOPC:string, 
            hwPath:string,  
            externalApi:ExternalApi, 
            runtimePackage:RuntimePackage,
            platformTarget:PlatformTarget, 
            hwDriver:HwDriveTarget, 
            timeSimutionMode:TimeSimutionMode, 
            timeoutCall:uint32) =
        { 
            DsFilePath = path
            HwIP = hwIP
            HwOPC = hwOPC
            HwPath = hwPath
            ExternalApi = externalApi
            RuntimePackage = runtimePackage
            PlatformTarget = platformTarget
            HwDriver = hwDriver
            TimeSimutionMode = timeSimutionMode
            TimeoutCall = timeoutCall
        }
    let createModelConfigReplacePath (cfg:ModelConfig, path:string) =
        { cfg with DsFilePath = path }
    let createModelConfigReplacePackage (cfg:ModelConfig, runtimePackage:RuntimePackage) =
        { cfg with RuntimePackage = runtimePackage }

    type RuntimeDS() =
        static member val System : ISystem option = None with get, set
        static member val ModelConfig : ModelConfig = createDefaultModelConfig() with get, set
        
        //RuntimePackage는 외부에서 변경가능 
        static member ChangeRuntimePackage(package:RuntimePackage) =
            RuntimeDS.ModelConfig <- createModelConfigWithSimMode(RuntimeDS.ModelConfig, package) 

    let getFullSlotHwSlotDataTypes() =
        let hw =
            [|0 .. 11|]
            |> Array.map (fun i ->
                if i % 2 = 0 then
                    SlotDataType(i, IOType.In, DataType.DuUINT64)
                else
                    SlotDataType(i, IOType.Out, DataType.DuUINT64))
        hw

    let getDefaltHwTarget() = HwTarget(WINDOWS, OPC_IO, getFullSlotHwSlotDataTypes())

module PlatformTargetExtensions =
        let fromString s =
            match s with
            | "WINDOWS" | "WINDOWS PC"-> WINDOWS
            | "XGI"  | "LS Electric XGI PLC"  -> XGI
            | "XGK"  | "LS Electric XGK PLC"  -> XGK
            | _ -> failwithf $"Error ToPlatformTarget: {s}"

        let allPlatforms =
            [ WINDOWS; XGI; XGK;]

        let getAlias x =
            match x with
            | WINDOWS -> "WINDOWS PC"
            | XGI -> "LS Electric XGI PLC"
            | XGK -> "LS Electric XGK PLC"



module RuntimePackageExtensions =
        let fromString s =
            match s with
            | "Simulation"-> Simulation
            | "Control" -> Control
            | "Monitoring" -> Monitoring
            | "VirtualPlant" -> VirtualPlant
            | "VirtualLogic" -> VirtualLogic
            | _ -> failwithf $"Error PlatformTarget: {s}"

        let allRuntimePackage =
            [ Simulation; Control; Monitoring; VirtualPlant; VirtualLogic]
            

module ExternalApiExtensions =
    let fromString s =
            match s with
            | "OPC"  -> ExternalApi.OPC
            | "REDIS"  -> ExternalApi.REDIS
            | _ -> failwithf $"Error ExternalApi: {s}"

    let allExternalApi =
        [ OPC; REDIS;]

module HwDriveTargetExtensions =
    let fromString s =
            match s with
            | "LS_XGI_IO"  -> LS_XGI_IO
            | "LS_XGK_IO"  -> LS_XGK_IO
            | "MELSEC_IO"  -> MELSEC_IO
            | "OPC_IO" 
            | _            -> OPC_IO


    let allDrivers =
        [ LS_XGI_IO; LS_XGK_IO;  MELSEC_IO;  OPC_IO ]


module TimeSimutionModeExtensions =

        let toString mode =
            match mode with
            | TimeNone -> "Ignore Time"
            | TimeX0_1 -> "0.1x Speed"
            | TimeX0_5 -> "0.5x Speed"
            | TimeX1 -> "1x Speed"
            | TimeX2 -> "2x Speed"
            | TimeX4 -> "4x Speed"
            | TimeX8 -> "8x Speed"
            | TimeX16 -> "16x Speed"
            | TimeX100 -> "100x Speed"

        let fromString s =
            match s with
            | "Ignore Time" -> TimeNone
            | "0.1x Speed" -> TimeX0_1
            | "0.5x Speed" -> TimeX0_5
            | "1x Speed" -> TimeX1
            | "2x Speed" -> TimeX2
            | "4x Speed" -> TimeX4
            | "8x Speed" -> TimeX8
            | "16x Speed" -> TimeX16
            | "100x Speed" -> TimeX100
            | _ -> failwithf $"Error ToTimeSimutionMode: {s}"

        let allModes =
            [ TimeNone; TimeX0_1; TimeX0_5; TimeX1; TimeX2; TimeX4; TimeX8; TimeX16; TimeX100 ]
