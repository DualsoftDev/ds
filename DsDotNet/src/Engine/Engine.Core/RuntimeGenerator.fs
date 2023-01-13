namespace Engine.Core

open System.Reactive.Subjects
open Engine.Common.FS

[<AutoOpen>]
module RuntimeGeneratorModule =
    type RuntimeTargetType = WINDOWS | XGI | XGK | AB | MELSEC

    type Runtime() =
        static let mutable runtimeTarget = WINDOWS
        static let targetChangedSubject = new Subject<RuntimeTargetType>()
        static member Target
            with get() = runtimeTarget
            and set(v) =
                //if v <> runtimeTarget then
                runtimeTarget <- v
                targetChangedSubject.OnNext(v)
        static member TargetChangedSubject = targetChangedSubject

