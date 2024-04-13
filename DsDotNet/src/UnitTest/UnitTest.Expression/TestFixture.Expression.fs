namespace T

open Dual.UnitTest.Common.FS
open Dual.Common.Core.FS
open Engine.Core
open Engine.Parser.FS

[<AutoOpen>]
module ExpressionFixtures =
    let sys = DsSystem("testSys")
    let mutable runtimeTarget = WINDOWS
    let setRuntimeTarget(target:PlatformTarget) =
            let runtimeTargetBackup = target
            RuntimeDS.System <- sys
            runtimeTarget <- target
            ParserUtil.runtimeTarget <-target
            disposable { runtimeTarget <- runtimeTargetBackup }

    [<AbstractClass>]
    type ExpressionTestBaseClass() =
        inherit TestBaseClass("EngineLogger")
        do
            //Engine.CodeGenCPU.ModuleInitializer.Initialize()
            setRuntimeTarget XGI |> ignore
