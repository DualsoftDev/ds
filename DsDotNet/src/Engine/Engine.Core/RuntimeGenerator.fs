namespace Engine.Core

open System.Reactive.Subjects
open Dual.Common.Core.FS
open System.Collections.Generic

[<AutoOpen>]
module RuntimeGeneratorModule =

    type RuntimeTargetType = 
        | WINDOWS 
        | XGI 
        | XGK 
        | AB 
        | MELSEC

    type RuntimePackage = 
        | PC 
        | PLC 
        | Emulation 
        | Simulation 
        | Developer 
    with
        member x.IsPackagePC() =
            match x with
            | PC  -> true
            | _ -> false

        member x.IsPackagePLC() =
            match x with
            | PLC  -> true
            | _ -> false

        member x.IsPackageEmulation() =
            match x with
            | Emulation  -> true
            | _ -> false

        member x.IsPackageSIM() =
            match x with
            | Simulation | Developer -> true
            | _ -> false

    let RuntimePackageList = [ PC; PLC; Emulation;  Simulation; Developer]

    let ToRuntimePackage s =
        match s with
        | "PC" -> PC
        | "PLC" -> PLC
        | "Emulation" -> Emulation
        | "Simulation" -> Simulation
        | "Developer" -> Developer
        | _ -> failwithlogf $"Error {getFuncName()}"

    let InitStartMemory = 1000
    let BufferAlramSize = 1000
    let ExternalTempMemory =  "%MX0"
   
    type RuntimeDS() =
        static let mutable runtimeTarget = WINDOWS
        static let mutable runtimePackage = Simulation
        static let targetChangedSubject = new Subject<RuntimeTargetType>()
        static let packageChangedSubject = new Subject<RuntimePackage>()
        static let mutable dsSystem: ISystem option = None
        static let mutable callTimeout = 15000u
        static let mutable emulationAddress = ""

        static member Target
            with get() = runtimeTarget
            and set v =
                runtimeTarget <- v
                targetChangedSubject.OnNext(v)

        static member val HwSlotDataTypes  =  ResizeArray<SlotDataType>() with get, set
        static member val HwStartInBit = 0   with get, set
        static member val HwStartOutBit = 0  with get, set
        static member val IP = "192.168.9.100" with get, set

        static member val TimeoutCall = callTimeout  with get, set
        static member val EmulationAddress = emulationAddress  with get, set
        static member TargetChangedSubject = targetChangedSubject

        static member Package
            with get() = runtimePackage
            and set v =
                runtimePackage <- v
                packageChangedSubject.OnNext(v)

        static member PackageChangedSubject = packageChangedSubject

        static member System
            with get() = dsSystem.Value
            and set v = dsSystem <- Some v
