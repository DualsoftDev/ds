namespace T

open Engine.Common.FS
open Engine.Core

[<AutoOpen>]
module ExpressionFixtures =
    let setRuntimeTarget(runtimeTarget:RuntimeTargetType) =
        let runtimeTargetBackup = Runtime.Target
        Runtime.Target <- runtimeTarget
        disposable { Runtime.Target <- runtimeTargetBackup }

    [<AbstractClass>]
    type ExpressionTestBaseClass() =
        inherit TestBaseClass("EngineLogger")
        do
            Engine.CodeGenCPU.ModuleInitializer.Initialize()
