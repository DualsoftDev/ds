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
        | Simulation 
        | StandardPC 
        | StandardPLC 
        | LightPC 
        | LightPLC
    with
        member x.IsPackagePC() =
            match x with
            | StandardPC | LightPC -> true
            | _ -> false

        member x.IsPackagePLC() =
            match x with
            | StandardPLC | LightPLC -> true
            | _ -> false

        member x.IsPackageSIM() =
            match x with
            | Simulation -> true
            | _ -> false

    let RuntimePackageList = [Simulation; StandardPC; StandardPLC; LightPC; LightPLC]

    let ToRuntimePackage s =
        match s with
        | "Simulation" -> Simulation
        | "StandardPC" -> StandardPC
        | "StandardPLC" -> StandardPLC
        | "LightPC" -> LightPC
        | "LightPLC" -> LightPLC
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
