namespace T

open System.IO
open log4net
open log4net.Config
open Engine.Common.FS
open Engine.Core

// FsUnit/XUnit 사용법:
// https://github.com/fsprojects/FsUnit/tree/master/tests/FsUnit.Xunit.Test
// https://marnee.silvrback.com/fsharp-and-xunit-classfixture
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
