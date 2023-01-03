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
module Fixtures =
    let configureLog4Net (loggerName:string) log4netConfigFile =
        XmlConfigurator.Configure(new FileInfo(log4netConfigFile)) |> ignore
        let logger = LogManager.GetLogger(loggerName)
        Engine.Common.Global.Logger <- logger
        gLogger <- logger
        logger

    let SetUpTest() =

            let cwd = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), @"..\..\..\"))
            sprintf "테스트 초기화 수행" |> ignore
            let configFile = $@"{cwd}App.config"
            let logger = configureLog4Net "EngineLogger" configFile

            // 로깅 결과 파일 : UnitTest.Engine/bin/logEngine*.txt
            logInfo "Log4net logging enabled!!!"

            if not (File.Exists configFile) then
                failwith "config 파일 위치를 강제로 수정해 주세요."
            ()

            Engine.CodeGenCPU.ModuleInitializer.Initialize()

    let setRuntimeTarget(runtimeTarget:RuntimeTarget) =
        let runtimeTargetBackup = RuntimeTarget
        RuntimeTarget <- runtimeTarget
        disposable { RuntimeTarget <- runtimeTargetBackup }

