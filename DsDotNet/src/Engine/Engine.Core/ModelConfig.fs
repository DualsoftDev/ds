namespace Engine.Core

open System.Reactive.Subjects
open Dual.Common.Core.FS
open System.Collections.Generic
open Dual.Common.Base.FS.Functions
open MapperDataModule
open System.Runtime.Serialization
open System.Xml
open System.IO
open Newtonsoft.Json

[<AutoOpen>]
module ModelConfigModule =

    
    let InitStartMemory = 1000
    let OpModeLampBtnMemorySize = 100 ////OP 조작 LAMP 100개만 지원
    
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


    type RuntimeMode =
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

    let ToRuntimeMode s =
        match s with
        | "Simulation" -> Simulation
        | "Control" -> Control
        | "Monitoring" -> Monitoring
        | "VirtualPlant" -> VirtualPlant
        | "VirtualLogic" -> VirtualLogic
        | _ -> Simulation

    //제어 HW CPU 기기 타입

    type HwCPU =
        | WINDOWS
        | XGI
        | XGK
        with 
            member x.IsPLC = 
                x = XGI || x = XGK

    //제어 Driver IO 기기 타입
    type HwIO =
        | LS_XGI_IO
        | LS_XGK_IO
        | MELSEC_IO
        | OPC_IO

    //제어 Driver IO 기기 타입
    type ExternalApi =
        | OPC
        | REDIS

    //HW CPU,  Driver IO, Slot 정보 조합
    type HwTarget(hwCPU:HwCPU, hwIO:HwIO, slots:SlotDataType[], startM:int) =
        member x.HwCPU = hwCPU
        member x.HasSlot = slots.any(fun a -> a.IOType <> IOType.NotUsed)
        member x.HwIO = 
            match hwCPU with
            | WINDOWS -> hwIO
            | XGI -> LS_XGI_IO
            | XGK -> LS_XGK_IO

        member val Slots = slots with get, set
        member val StartMemory = startM with get, set


    let defaultHwTarget = 
        let slot0 =   SlotDataType(0, IOType.In, DataType.DuUINT64)
        let slot1 =   SlotDataType(1, IOType.Out, DataType.DuUINT64)
        HwTarget(HwCPU.WINDOWS, HwIO.LS_XGI_IO, [|slot0; slot1|], 1000)

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


    let getExternalTempMemory (target:HwTarget, index:int) =
        match target.HwCPU with
        | XGI-> ExternalTempIECMemory+index.ToString()
        | XGK-> ExternalTempNoIECMemory+index.ToString("00000")
        | WINDOWS-> ExternalTempMemory+($"{index/8}.{index%8}")

    type ModelConfig = {
        DsFilePath: string
        HwIP: string
        HwOPC : string
        TagConfig : TagConfig   
        ExternalApi: ExternalApi
        RuntimeMode: RuntimeMode
        TimeSimutionMode : TimeSimutionMode
        TimeoutCall : uint32
        HwTarget : HwTarget 
    }
    with    
        member x.HwCPU = x.HwTarget.HwCPU
        member x.Slots = x.HwTarget.Slots

        member x.ToMessage() = 
            let baseMsg = $"RuntimeMode: {x.RuntimeMode}"
            let hwIOInfo = 
                match x.HwTarget.HwIO with
                | LS_XGI_IO -> x.HwIP
                | LS_XGK_IO -> x.HwIP
                | MELSEC_IO -> x.HwIP
                | OPC_IO -> x.HwOPC
            let hwIOHwCPU = 
                match x.HwCPU with
                | WINDOWS -> hwIOInfo
                | XGI -> "LS_XGI_IO"
                | XGK -> "LS_XGK_IO"

            match x.RuntimeMode with
            | Simulation ->
                baseMsg + $"\r\nTimeSimutionMode: {x.TimeSimutionMode}"
            | Control -> 
                baseMsg + $"\r\nHwCPU: {x.HwCPU}
                \r\nHwDriver: {x.HwTarget.HwIO} ({hwIOHwCPU}) 
                \r\nTimeoutCall: {x.TimeoutCall}"
            | Monitoring 
            | VirtualPlant -> 
                baseMsg + $"\r\nHwDriver: {x.HwTarget.HwIO}({hwIOInfo})
                \r\nTimeoutCall: {x.TimeoutCall}"
            | VirtualLogic -> 
                baseMsg + $"\r\nTimeSimutionMode: {x.TimeSimutionMode}\r\nExternalApi: {x.ExternalApi}
                \r\nTimeoutCall: {x.TimeoutCall}"


    let createDefaultModelConfig() =
        { 
            DsFilePath = ""
            HwIP = "127.0.0.1"
            HwOPC = "opc.tcp://127.0.0.1:2747"
            TagConfig = createDefaultTagConfig()    
            ExternalApi = ExternalApi.OPC
            RuntimeMode = Simulation //unit test를 위해 Simulation으로 설정
            TimeSimutionMode = TimeX1
            TimeoutCall = 15000u
            HwTarget = defaultHwTarget
        }

    let createModelConfig(path:string,
            hwIP:string, 
            hwOPC:string, 
            tagConfig:TagConfig, 
            externalApi:ExternalApi, 
            runtimeMode:RuntimeMode,
            hwTarget:HwTarget,
            timeSimutionMode:TimeSimutionMode, 
            timeoutCall:uint32) =
        { 
            DsFilePath = path
            HwIP = hwIP
            HwOPC = hwOPC
            TagConfig = tagConfig
            ExternalApi = externalApi
            RuntimeMode = runtimeMode
            TimeSimutionMode = timeSimutionMode
            TimeoutCall = timeoutCall
            HwTarget = hwTarget
        }
    let createModelConfigReplacePath (cfg:ModelConfig, path:string) =
        { cfg with DsFilePath = path }
    let createModelConfigReplaceRuntimeMode (cfg:ModelConfig, runtimeMode:RuntimeMode) =
        { cfg with RuntimeMode = runtimeMode }
    let createModelConfigReplaceHwCPU(cfg:ModelConfig, hwCPU:HwCPU) =
        let newHwTarget = new HwTarget(hwCPU,  cfg.HwTarget.HwIO , cfg.HwTarget.Slots,  cfg.HwTarget.StartMemory )
        { cfg with HwTarget = newHwTarget }
    let createModelConfigWithHwConfig(config: ModelConfig,  hwTarget:HwTarget) =
        { config with  HwTarget = hwTarget }
    let createModelConfigWithHwIp(config: ModelConfig,  ip:string, tagConfig:TagConfig) =
        { config with  HwIP = ip; TagConfig = tagConfig }

    type RuntimeParam =
        {
            System: ISystem option
            RuntimeMode: RuntimeMode
            TimeSimutionMode: TimeSimutionMode
        }

    type RuntimeDS() =
        static member val Param = 
                            {
                                System = None
                                RuntimeMode = Simulation
                                TimeSimutionMode = TimeX1
                            } with get, set

        static member ReplaceSystem(sys: ISystem) =
            RuntimeDS.Param <- { RuntimeDS.Param with System = Some sys }
        static member UpdateParam(runtimeMode:RuntimeMode, timeSimutionMode:TimeSimutionMode) =
            RuntimeDS.Param <-  {
                                    System = RuntimeDS.Param.System 
                                    RuntimeMode = runtimeMode
                                    TimeSimutionMode = timeSimutionMode
                                }

    let getFullSlotHwSlotDataTypes() =
        let hw =
            [|0 .. 11|]
            |> Array.map (fun i ->
                if i % 2 = 0 then
                    SlotDataType(i, IOType.In, DataType.DuUINT64)
                else
                    SlotDataType(i, IOType.Out, DataType.DuUINT64))
        hw

    //let getDefaltHwTarget() = defaultHwTarget

module HwCPUExtensions =
        let fromString s =
            match s with
            | "WINDOWS" | "WINDOWS PC"-> WINDOWS
            | "XGI"  | "LS Electric XGI PLC"  -> XGI
            | "XGK"  | "LS Electric XGK PLC"  -> XGK
            | _ -> failwithf $"Error ToHwCPU: {s}"

        let allPlatforms =
            [ WINDOWS; XGI; XGK;]

        let getAlias x =
            match x with
            | WINDOWS -> "WINDOWS PC"
            | XGI -> "LS Electric XGI PLC"
            | XGK -> "LS Electric XGK PLC"

module RuntimeModeExtensions =
        let fromString s =
            match s with
            | "Simulation"-> Simulation
            | "Control" -> Control
            | "Monitoring" -> Monitoring
            | "VirtualPlant" -> VirtualPlant
            | "VirtualLogic" -> VirtualLogic
            | _ -> failwithf $"Error HwCPU: {s}"

        let allRuntimeMode =
            [ Simulation; Control; Monitoring; VirtualPlant; VirtualLogic]

module ExternalApiExtensions =
    let fromString s =
            match s with
            | "OPC"  -> ExternalApi.OPC
            | "REDIS"  -> ExternalApi.REDIS
            | _ -> failwithf $"Error ExternalApi: {s}"

    let allExternalApi =
        [ OPC; REDIS;]

module HwIOExtensions =
    let fromString s =
            match s with
            | "LS_XGI_IO"  -> LS_XGI_IO
            | "LS_XGK_IO"  -> LS_XGK_IO
            | "MELSEC_IO"  -> MELSEC_IO
            | "OPC_IO"     -> OPC_IO
            | _ -> failwithf $"Error HwIO: {s}"


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

