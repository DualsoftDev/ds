namespace Dual.Common.UnitTest.FS

open System.IO
open log4net
open log4net.Config
open Dual.Common.Core.FS
open type Dual.Common.Base.CS.DcLogger

// FsUnit/XUnit 사용법:
// https://github.com/fsprojects/FsUnit/tree/master/tests/FsUnit.Xunit.Test
// https://marnee.silvrback.com/fsharp-and-xunit-classfixture
[<AutoOpen>]
module Fixtures =
    let private configureLog4Net (loggerName:string) log4netConfigFile =
        XmlConfigurator.Configure(new FileInfo(log4netConfigFile)) |> ignore
        let logger = LogManager.GetLogger(loggerName)
        Logger <- logger
        logger

    let mutable private _setupDone = false
    let private enableLogger (appConfigPath:string) (loggerName:string) =
        if not <| loggerName.IsNullOrEmpty() && not _setupDone then
            let _logger = configureLog4Net loggerName appConfigPath

            // 로깅 결과 파일 : UnitTest.Engine/bin/logEngine*.txt
            logInfo "Log4net logging enabled!!!"

            if not (File.Exists appConfigPath) then
                failwithlog $"config 파일 {appConfigPath} 위치를 강제로 수정해 주세요."
            _setupDone <- true

    //let createTag(name, address, value) =
    //    let param = {
    //        Name = name
    //        Value = value
    //        Address = Some address
    //        Comment = None
    //        System = Runtime.System
    //        Target = None
    //        TagKind = -1
    //        IsGlobal = false
    //    }
    //    Tag(param)

    //let parseText (systemRepo:ShareableSystemRepository) referenceDir text =
    //    let helper = ModelParser.ParseFromString2(text, ParserOptions.Create4Simulation(systemRepo, referenceDir, "ActiveCpuName", None, DuNone))
    //    helper.TheSystem

    [<AbstractClass>]
    type TestClassWithLogger(appConfigPath:string, loggerName:string) =
        do
            enableLogger appConfigPath loggerName
            EnableTrace <- true
        member val Locker = obj
