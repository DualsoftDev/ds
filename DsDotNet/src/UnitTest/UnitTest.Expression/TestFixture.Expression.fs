namespace T

open System.IO
open Dual.UnitTest.Common.FS
open Dual.Common.Core.FS
open Engine.Core
open Engine.Parser.FS


[<AutoOpen>]
module ExpressionFixtures =
    let tryParseStatement4UnitTest (targetType:PlatformTarget) (storages: Storages) (text: string) : Statement option =
        try
            let parser = ExpressionParserModule.createParser (text)
            let ctx = parser.statement ()
            let parserData = new ParserData(targetType, storages, Some parser)

            tryCreateStatement parserData ctx
        with exn ->
            failwith $"Failed to parse Statement: {text}\r\n{exn}"


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
        inherit TestClassWithLogger(Path.Combine($"{__SOURCE_DIRECTORY__}/App.config"), "UnitTestLogger")
        do
            //Engine.CodeGenCPU.ModuleInitializer.Initialize()
            setRuntimeTarget XGI |> ignore
