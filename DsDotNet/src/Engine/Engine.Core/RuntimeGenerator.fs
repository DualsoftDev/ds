namespace Engine.Core

open System.Reactive.Subjects
open Dual.Common.Core.FS

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

    type RuntimeDS() =
        static let mutable runtimeTarget = WINDOWS
        static let mutable runtimePackage = Simulation
        static let targetChangedSubject = new Subject<RuntimeTargetType>()
        static let packageChangedSubject = new Subject<RuntimePackage>()
        static let mutable dsSystem: ISystem option = None
        static let mutable callTimeout = 10000us

        static member Target
            with get() = runtimeTarget
            and set v =
                runtimeTarget <- v
                targetChangedSubject.OnNext(v)

        static member val HwBlockSizeIn  = DataType.DuUINT64 with get, set
        static member val HwBlockSizeOut = DataType.DuUINT64 with get, set
        static member val HwStartInDINT = 1   with get, set
        static member val HwStartOutDINT = 1  with get, set
        static member val HwStartMemoryDINT = 1000u  with get, set
        static member val IP = "192.168.9.100" with get, set

        static member TimeoutCall = callTimeout
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
