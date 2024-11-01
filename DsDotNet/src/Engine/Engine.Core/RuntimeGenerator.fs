namespace Engine.Core

open System.Reactive.Subjects
open Dual.Common.Core.FS
open System.Collections.Generic
open Dual.Common.Base.FS.Functions

[<AutoOpen>]
module RuntimeGeneratorModule =

    //제어 HW CPU 기기 타입
    type PlatformTarget =
        | WINDOWS
        | XGI
        | XGK
        | AB
        | MELSEC
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
        | AB_IO
        | MELSEC_IO
        | SIEMENS_IO
        | PAIX_IO

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
    let XGKAnalogOffsetByte = 128
    let XGKAnalogOutOffsetByte = 128


    let ExternalTempMemory =  "M"
    let ExternalTempIECMemory =  "%MX"
    let ExternalTempNoIECMemory =  "M"


    let HMITempMemory =  "%HX99"  //iec xgk 구분안함
    let HMITempManualAction =  "%HX0"  //iec xgk 구분안함


    let getExternalTempMemory (target:HwTarget, index:int) =
        match target.Platform with
        | XGI-> ExternalTempIECMemory+index.ToString()
        | XGK-> ExternalTempNoIECMemory+index.ToString("00000")
        | WINDOWS-> ExternalTempMemory+($"{index/8}.{index%8}")
        | _ -> failwithlog $"{target} not support"

    type RuntimeDS() =
        static let mutable runtimePackage = PCSIM
        static let packageChangedSubject = new Subject<RuntimePackage>()
        static let mutable dsSystem: ISystem option = None
        static let mutable runtimeMotionMode = RuntimeMotionMode.MotionSync
        static let mutable timeSimutionMode = TimeSimutionMode.TimeX1
        static let mutable callTimeout = 15000u
        static let mutable emulationAddress = ""

        static member val HwIP = "192.168.9.100" with get, set
        static member val HwDriver = HwDriveTarget.LS_XGK_IO with get, set //PC 제어시 Hw maker 별 이름 (지금은 LS 태그타입 구분용)

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

        static member val System = dsSystem with get, set


    let getFullSlotHwSlotDataTypes() =
        let hw =
            [|0 .. 11|]
            |> Array.map (fun i ->
                if i % 2 = 0 then
                    SlotDataType(i, IOType.In, DataType.DuUINT64)
                else
                    SlotDataType(i, IOType.Out, DataType.DuUINT64))
        hw

    let getDefaltHwTarget() = HwTarget(WINDOWS, PAIX_IO, getFullSlotHwSlotDataTypes())

module PlatformTargetExtensions =
        let fromString s =
            match s with
            | "WINDOWS"-> WINDOWS
            | "XGI"    -> XGI
            | "XGK"    -> XGK
            | "AB"     -> AB
            | "MELSEC" -> MELSEC
            | _ -> failwithf $"Error ToPlatformTarget: {s}"

        let allPlatforms =
            [ WINDOWS; XGI; XGK; AB; MELSEC]


module HwDriveTargetExtensions =
    let fromString s =
            match s with
            | "LS_XGI_IO"  -> LS_XGI_IO
            | "LS_XGK_IO"  -> LS_XGK_IO
            | "AB_IO"      -> AB_IO
            | "MELSEC_IO"  -> MELSEC_IO
            | "SIEMENS_IO" -> SIEMENS_IO
            | "PAIX_IO"    -> PAIX_IO
            | _ -> failwithf $"Error ToHwDriveTarget: {s}"

    let allDrivers =
        [ LS_XGI_IO; LS_XGK_IO; AB_IO; MELSEC_IO; SIEMENS_IO; PAIX_IO ]


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
