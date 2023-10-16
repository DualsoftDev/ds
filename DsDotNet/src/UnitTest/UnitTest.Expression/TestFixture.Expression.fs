namespace T

open Dual.UnitTest.Common.FS
open Dual.Common.Core.FS
open Engine.Core

[<AutoOpen>]
module ExpressionFixtures =
    let sys = DsSystem("testSys", "localhost")
    let setRuntimeTarget(runtimeTarget:RuntimeTargetType) =
        let runtimeTargetBackup = RuntimeDS.Target
        RuntimeDS.Target <- runtimeTarget
        RuntimeDS.System <- sys
        disposable { RuntimeDS.Target <- runtimeTargetBackup }


    [<AbstractClass>]
    type ExpressionTestBaseClass() =
        inherit TestBaseClass("EngineLogger")
        do
            //Engine.CodeGenCPU.ModuleInitializer.Initialize()
            setRuntimeTarget XGI |> ignore
