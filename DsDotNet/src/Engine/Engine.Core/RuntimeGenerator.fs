namespace Engine.Core

open System.Reactive.Subjects
open Dual.Common.Core.FS

[<AutoOpen>]
module RuntimeGeneratorModule =
    type RuntimeTargetType = WINDOWS | XGI | XGK | AB | MELSEC
    type RuntimePackage    = Simulation | StandardPC | StandardPLC | LightPC | LightPLC
        with
            member x.IsPackagePC() =
                match x with
                | StandardPC | LightPC -> true
                | _ -> false
            member x.IsPackagePLC() =
                match x with
                | StandardPLC | StandardPLC -> true
                | _ -> false
            member x.IsPackageSIM() =
                match x with
                | Simulation -> true
                | _ -> false

    let RuntimePackageList =  [ Simulation; StandardPC; StandardPLC; LightPC; LightPLC]
    let ToRuntimePackage(s:string) =
                match s with
                | "Simulation" -> Simulation
                | "StandardPC" -> StandardPC
                | "StandardPLC" -> StandardPLC
                | "LightPC" -> LightPC
                | "LightPLC" -> LightPLC
                | _-> failwithlog $"Error {getFuncName()}"    

    type Runtime() =
        static let mutable runtimeTarget = WINDOWS
        static let mutable runtimePackage = Simulation
        static let targetChangedSubject = new Subject<RuntimeTargetType>()
        static let packageChangedSubject = new Subject<RuntimePackage>()
        static let mutable dsSystem:ISystem option = None
        static member Target
            with get() = runtimeTarget
            and set(v) =
                //if v <> runtimeTarget then
                runtimeTarget <- v
                targetChangedSubject.OnNext(v)
        static member TargetChangedSubject = targetChangedSubject
        static member Package
            with get() = runtimePackage
            and set(v) =
                //if v <> runtimeTarget then
                runtimePackage <- v
                packageChangedSubject.OnNext(v)
        static member PackageChangedSubject = packageChangedSubject
        static member System
            with get() = dsSystem.Value
            and set(v) = dsSystem <- Some v

          
