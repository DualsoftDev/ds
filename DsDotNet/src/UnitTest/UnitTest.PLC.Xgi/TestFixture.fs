namespace T

open System.IO
open System.Globalization

open NUnit.Framework
open log4net
open log4net.Config

open Engine.Common.FS
open Engine.Core
open PLC.CodeGen.Common.QGraph
open PLC.CodeGen.LSXGI
open PLC.CodeGen.LSXGI.Config.POU.Program.LDRoutine

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



[<AutoOpen>]
module PLCGenerationTestModule =
    [<AbstractClass>]
    type PLCGenerationTestClass() =
        do Fixtures.SetUpTest()
        let mutable runtimeTarget = Runtime.Target
        let xgiGenerationOptionsBackup = xgiGenerationOptions
        [<SetUp>]
        member x.Setup () =
            Runtime.Target <- x.GetCurrentRuntimeTarget()
            xgiGenerationOptions <- {EnableXmlComment = false; IsAppendExpressionTextToRungComment = true}

        [<TearDown>]
        member __.TearDown () =
            Runtime.Target <- runtimeTarget
            xgiGenerationOptions <- xgiGenerationOptionsBackup

        abstract GetCurrentRuntimeTarget: unit -> RuntimeTargetType

    let setRuntimeTarget(runtimeTarget:RuntimeTargetType) =
        let runtimeTargetBackup = Runtime.Target
        Runtime.Target <- runtimeTarget
        disposable { Runtime.Target <- runtimeTargetBackup }

[<AutoOpen>]
module XgiGenerationTestModule =
    let projectDir =
        let src = __SOURCE_DIRECTORY__
        let key = @"UnitTest\UnitTest.PLC.Xgi"
        let tail = src.IndexOf(key) + key.Length
        src.Substring(0, tail)
    let xmlDir = Path.Combine(projectDir, "XgiXmls")
    let xmlAnswerDir = Path.Combine(xmlDir, "Answers")

    let saveTestResult testFunctionName (xml:string) =
        let crlfXml = xml.Replace("\r\n", "\n").Replace("\n", "\r\n")
        File.WriteAllText($@"{xmlDir}\{testFunctionName}.xml", crlfXml)
        let answerXml = File.ReadAllText($@"{xmlAnswerDir}\{testFunctionName}.xml")
        System.String.Compare(answerXml, xml, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase ||| CompareOptions.IgnoreSymbols) === 0

    let codeForBits = """
        bool x00 = createTag("%IX0.0.0", false);
        bool x01 = createTag("%IX0.0.1", false);
        bool x02 = createTag("%IX0.0.2", false);
        bool x03 = createTag("%IX0.0.3", false);
        bool x04 = createTag("%IX0.0.4", false);
        bool x05 = createTag("%IX0.0.5", false);
        bool x06 = createTag("%IX0.0.6", false);
        bool x07 = createTag("%IX0.0.7", false);

        bool x08 = createTag("%IX0.0.8", false);
        bool x09 = createTag("%IX0.0.9", false);
        bool x10 = createTag("%IX0.0.10", false);
        bool x11 = createTag("%IX0.0.11", false);
        bool x12 = createTag("%IX0.0.12", false);
        bool x13 = createTag("%IX0.0.13", false);
        bool x14 = createTag("%IX0.0.14", false);
        bool x15 = createTag("%IX0.0.15", false);
"""

    let codeForBits31 = codeForBits + """
        bool x16 = createTag("%IX0.1.0", false);
        bool x17 = createTag("%IX0.1.1", false);
        bool x18 = createTag("%IX0.1.2", false);
        bool x19 = createTag("%IX0.1.3", false);
        bool x20 = createTag("%IX0.1.4", false);
        bool x21 = createTag("%IX0.1.5", false);
        bool x22 = createTag("%IX0.1.6", false);
        bool x23 = createTag("%IX0.1.7", false);

        bool x24 = createTag("%IX0.1.8", false);
        bool x25 = createTag("%IX0.1.9", false);
        bool x26 = createTag("%IX0.1.10", false);
        bool x27 = createTag("%IX0.1.11", false);
        bool x28 = createTag("%IX0.1.12", false);
        bool x29 = createTag("%IX0.1.13", false);
        bool x30 = createTag("%IX0.1.14", false);
        bool x31 = createTag("%IX0.1.15", false);
"""

type XgiTestClass() =
    inherit PLCGenerationTestClass()
    do
        autoVariableCounter <- 0

    override x.GetCurrentRuntimeTarget() = XGI
    member val Locker = obj

