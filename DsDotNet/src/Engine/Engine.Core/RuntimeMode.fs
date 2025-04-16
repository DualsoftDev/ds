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
    type HwTarget(platformTarget:PlatformTarget, hwIO:HwIO, slots:SlotDataType[]) =
        member x.PlatformTarget = platformTarget
        member x.HwIO = hwIO
        member val Slots = slots with get, set
        member val StartMemory = 1000 with get, set


    let createDefaultHwTarget() = 
        HwTarget(PlatformTarget.WINDOWS, HwIO.LS_XGI_IO, [||])

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


    let getExternalTempMemory (target:HwTarget, index:int) =
        match target.PlatformTarget with
        | XGI-> ExternalTempIECMemory+index.ToString()
        | XGK-> ExternalTempNoIECMemory+index.ToString("00000")
        | WINDOWS-> ExternalTempMemory+($"{index/8}.{index%8}")

    type ModelConfig = {
        DsFilePath: string
        HwIP: string
        HwOPC : string
        HwPath: string
        TagConfig : TagConfig   
        ExternalApi: ExternalApi
        RuntimePackage: RuntimePackage
        TimeSimutionMode : TimeSimutionMode
        TimeoutCall : uint32
        HwTarget : HwTarget
    }
    with    
        member x.PlatformTarget = x.HwTarget.PlatformTarget
        member x.HwIO = x.HwTarget.HwIO
        member x.Slots = x.HwTarget.Slots
        member x.UpdateTagConfig(tagConfig:TagConfig) = 
            x.TagConfig.DeviceApis.Clear()
            x.TagConfig.DeviceApis.AddRange(tagConfig.DeviceApis)
            x.TagConfig.UserMonitorTags.Clear()
            x.TagConfig.UserMonitorTags.AddRange(tagConfig.UserMonitorTags)
            x.TagConfig.DeviceTags.Clear()
            x.TagConfig.DeviceTags.AddRange(tagConfig.DeviceTags)

        member x.ToMessage() = 
            let baseMsg = $"DsFilePath: {x.DsFilePath}\r\nRuntimePackage: {x.RuntimePackage}"
            let hwIOInfo = 
                match x.HwIO with
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
                \r\nHwDriver: {x.HwIO} ({hwIOPlatformTarget}) 
                \r\nTimeoutCall: {x.TimeoutCall}"
            | Monitoring 
            | VirtualPlant -> 
                baseMsg + $"\r\nHwDriver: {x.HwIO}({hwIOInfo})
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
            TagConfig = createDefaultTagConfig()    
            ExternalApi = ExternalApi.OPC
            RuntimePackage = Simulation //unit test를 위해 Simulation으로 설정
            TimeSimutionMode = TimeX1
            TimeoutCall = 15000u
            HwTarget = createDefaultHwTarget()
        }
    let createModelConfigWithHwConfig(config: ModelConfig,  ip:string, tagConfig:TagConfig) =
        { config with  HwIP = ip; TagConfig = tagConfig }
    let createModelConfigWithSimMode(config: ModelConfig, package:RuntimePackage) =
        { config with RuntimePackage = package }
    let createModelConfig(path:string,
            hwIP:string, 
            hwOPC:string, 
            hwPath:string,  
            tagConfig:TagConfig, 
            externalApi:ExternalApi, 
            runtimePackage:RuntimePackage,
            hwTarget:HwTarget,
            timeSimutionMode:TimeSimutionMode, 
            timeoutCall:uint32) =
        { 
            DsFilePath = path
            HwIP = hwIP
            HwOPC = hwOPC
            HwPath = hwPath
            TagConfig = tagConfig
            ExternalApi = externalApi
            RuntimePackage = runtimePackage
            TimeSimutionMode = timeSimutionMode
            TimeoutCall = timeoutCall
            HwTarget = hwTarget
        }
    let createModelConfigReplacePath (cfg:ModelConfig, path:string) =
        { cfg with DsFilePath = path }
    let createModelConfigReplacePackage (cfg:ModelConfig, runtimePackage:RuntimePackage) =
        { cfg with RuntimePackage = runtimePackage }

    type RuntimeDS() =
        static member val System : ISystem option = None with get, set
        static member val RuntimePackage : RuntimePackage = Simulation with get, set
        static member val TimeSimutionMode : TimeSimutionMode = TimeX1 with get, set
        static member val IsPLC = false with get, set
        

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


module ModelConfigExtensions =


    //// ========== XML 문자열 직렬화 ==========
    //let ModelConfigToXmlText (config: ModelConfig) : string =
    //    let serializer = DataContractSerializer(typeof<ModelConfig>)
    //    use stringWriter = new StringWriter()
    //    use xmlWriter = XmlWriter.Create(stringWriter, XmlWriterSettings(Indent = true))
    //    serializer.WriteObject(xmlWriter, config)
    //    xmlWriter.Flush()
    //    stringWriter.ToString()

    //let XmlToModelConfig (xmlText: string) : ModelConfig =
    //    let serializer = DataContractSerializer(typeof<ModelConfig>)
    //    use stringReader = new StringReader(xmlText)
    //    use xmlReader = XmlReader.Create(stringReader)
    //    try
    //        serializer.ReadObject(xmlReader) :?> ModelConfig
    //    with _ ->
    //        createDefaultModelConfig()


    // ========== JSON 저장/불러오기 ==========

    let private jsonSettings = JsonSerializerSettings()
    let ModelConfigToJsonText (cfg: ModelConfig) : string =
        JsonConvert.SerializeObject(cfg, Formatting.Indented, jsonSettings)

    let ModelConfigFromJsonText (json: string) : ModelConfig =
        JsonConvert.DeserializeObject<ModelConfig>(json, jsonSettings)

