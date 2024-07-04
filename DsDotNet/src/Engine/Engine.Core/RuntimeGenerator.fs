namespace Engine.Core

open System.Reactive.Subjects
open Dual.Common.Core.FS
open System.Collections.Generic

[<AutoOpen>]
module RuntimeGeneratorModule =

    type PlatformTarget = 
        | WINDOWS 
        | XGI 
        | XGK 
        | AB 
        | MELSEC

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

    type RuntimePackage = 
        | PC 
        | PCSIM 
        | PLC
        | PLCSIM 
    with
        member x.IsPCorPCSIM() =
            match x with
            | PC | PCSIM  -> true
            | _ -> false

        member x.IsPLCorPLCSIM() =
            match x with
            | PLC | PLCSIM -> true
            | _ -> false

        member x.IsPackageSIM() =
            match x with
            | PCSIM | PLCSIM -> true
            | _ -> false

    let RuntimePackageList = [ PC;PCSIM; PLC; PLCSIM;  ]

    let ToRuntimePackage s =
        match s with
        | "PC" -> PC
        | "PCSIM" -> PCSIM
        | "PLC" -> PLC
        | "PLCSIM" -> PLCSIM
        | _ -> failwithlogf $"Error {getFuncName()}"

    let InitStartMemory = 1000
    let BufferAlramSize = 9999


    let ExternalTempMemory =  "M"
    let ExternalTempIECMemory =  "%MX"
    let ExternalTempNoIECMemory =  "M"


    let HMITempMemory =  "%HX99"  //iec xgk 구분안함
    let HMITempManualAction =  "%HX0"  //iec xgk 구분안함


    let getExternalTempMemory (target:PlatformTarget, index:int) =
        match target with
        | XGI -> ExternalTempIECMemory+index.ToString()
        | XGK -> ExternalTempNoIECMemory+index.ToString("00000")
        | WINDOWS  -> ExternalTempMemory+($"{index/8}.{index%8}")
        | AB 
        | MELSEC  -> failwithlog $"{target} not support"
   
    type RuntimeDS() =
        static let mutable runtimePackage = PCSIM
        static let packageChangedSubject = new Subject<RuntimePackage>()
        static let mutable dsSystem: ISystem option = None
        static let mutable runtimeMotionMode = RuntimeMotionMode.MotionSync
        static let mutable timeSimutionMode = TimeSimutionMode.TimeX1
        static let mutable callTimeout = 15000u
        static let mutable emulationAddress = ""

        static member val HwSlotDataTypes  =  ResizeArray<SlotDataType>() with get, set
        static member val IP = "192.168.9.100" with get, set

        static member val TimeoutCall = callTimeout  with get, set
        static member val EmulationAddress = emulationAddress  with get, set
        static member val RuntimeMotionMode = runtimeMotionMode  with get, set
        static member val TimeSimutionMode = timeSimutionMode  with get, set
        
        static member Package
            with get() = runtimePackage
            and set v =
                runtimePackage <- v
                packageChangedSubject.OnNext(v)

        static member PackageChangedSubject = packageChangedSubject

        static member System
            with get() = dsSystem.Value
            and set v = dsSystem <- Some v


    let clearNFullSlotHwSlotDataTypes() =
        let hw = 
            [0 .. 11]
            |> List.map (fun i ->
                if i % 2 = 0 then
                    (i, IOType.In, DataType.DuUINT64)
                else
                    (i, IOType.Out, DataType.DuUINT64))

        // 기존의 리스트를 지우고 새로운 데이터로 대체합니다.
        RuntimeDS.HwSlotDataTypes.Clear()
        RuntimeDS.HwSlotDataTypes.AddRange(hw)


        
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
