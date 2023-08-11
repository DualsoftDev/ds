namespace T

open Dual.Common.Core.FS
open Engine.Core

[<AutoOpen>]
module ExpressionFixtures =
    let sys = DsSystem("testSys", "localhost")
    let setRuntimeTarget(runtimeTarget:RuntimeTargetType) =
        let runtimeTargetBackup = Runtime.Target
        Runtime.Target <- runtimeTarget
        Runtime.System <- sys
        disposable { Runtime.Target <- runtimeTargetBackup }


    [<AbstractClass>]
    type ExpressionTestBaseClass() =
        inherit TestBaseClass("EngineLogger")
        do
            Engine.CodeGenCPU.ModuleInitializer.Initialize()
            setRuntimeTarget XGI |> ignore
