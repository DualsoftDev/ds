namespace T

open System.IO
open System.Globalization

open NUnit.Framework
open log4net
open log4net.Config

open Engine.Common.FS
open Engine.Core
open PLC.CodeGen.Common.QGraph

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
        let mutable runtimeTarget = RuntimeTarget
        [<SetUp>]
        member x.Setup () =
            RuntimeTarget <- x.GetCurrentRuntimeTarget()

        [<TearDown>]
        member __.TearDown () =
            RuntimeTarget <- runtimeTarget

        abstract GetCurrentRuntimeTarget: unit -> RuntimeTarget

    let setRuntimeTarget(runtimeTarget:RuntimeTarget) =
        let runtimeTargetBackup = RuntimeTarget
        RuntimeTarget <- runtimeTarget
        disposable { RuntimeTarget <- runtimeTargetBackup }


[<AutoOpen>]
module XgiGenerationTestModule =
    let projectDir =
        let src = __SOURCE_DIRECTORY__
        let key = @"UnitTest\UnitTest.PLC.Xgi"
        let tail = src.IndexOf(key) + key.Length
        src.Substring(0, tail)
    let xmlDir = Path.Combine(projectDir, "XgiXmls")
    let xmlAnswerDir = Path.Combine(xmlDir, "Answers")

    let saveTestResult testFunctionName xml =
        File.WriteAllText($@"{xmlDir}\{testFunctionName}.xml", xml)
        let answerXml = File.ReadAllText($@"{xmlAnswerDir}\{testFunctionName}.xml")
        System.String.Compare(answerXml, xml, CultureInfo.CurrentCulture, CompareOptions.IgnoreCase ||| CompareOptions.IgnoreSymbols) === 0

    let plcCodeGenerationOption =
        let n (v:IVertex) = v :?> INamed |> name
        /// 테스트 용으로 출력 신호를 따로 생성함.  e.g "Ap" --> "QAp"
        let coilGenerator          = fun (v:IVertex) -> $"O_{n v}"
        let SensorGenerator        = fun (v:IVertex) -> $"I_{n v}"
        let runningGenerator       = fun (v:IVertex) -> $"{n v}_G"
        let finishGenerator        = fun (v:IVertex) -> $"{n v}_F"
        let resetGenerator         = fun (v:IVertex) -> $"{n v}_RST"
        let readyStateGenerator    = fun (v:IVertex) -> $"{n v}_R"
        let HomingStateGenerator   = fun (v:IVertex) -> $"{n v}_H"
        let OriginStateGenerator   = fun (v:IVertex) -> $"{n v}_O"
        let goinglockNameGenerator = fun (v:IVertex) (i:int) -> $"{n v}_GL{id}"

        { createDefaultCodeGenerationOption() with
            CoilTagGenerator            = Some coilGenerator
            SensorTagGenerator          = Some SensorGenerator
            GoingStateNameGenerator     = Some runningGenerator
            StandbyStateNameGenerator   = Some readyStateGenerator
            HomingStateNameGenerator    = Some HomingStateGenerator
            OriginStateNameGenerator    = Some OriginStateGenerator
            ResetLockRelayNameGenerator = Some goinglockNameGenerator
            ResetNameGenerator          = Some resetGenerator
            RelayGenerator              = relayGenerator "RR" 1// XGI 고려한 모델 : Relay 이름 R 대신 RR
            //FinishStateNameGenerator  = Some finishGenerator
        }

    let codeForBits = """
        bool myBit00 = createTag("%IX0.0.0", false);
        bool myBit01 = createTag("%IX0.0.1", false);
        bool myBit02 = createTag("%IX0.0.2", false);
        bool myBit03 = createTag("%IX0.0.3", false);
        bool myBit04 = createTag("%IX0.0.4", false);
        bool myBit05 = createTag("%IX0.0.5", false);
        bool myBit06 = createTag("%IX0.0.6", false);
        bool myBit07 = createTag("%IX0.0.7", false);

        bool myBit10 = createTag("%IX0.0.8", false);
        bool myBit11 = createTag("%IX0.0.9", false);
        bool myBit12 = createTag("%IX0.0.10", false);
        bool myBit13 = createTag("%IX0.0.11", false);
        bool myBit14 = createTag("%IX0.0.12", false);
        bool myBit15 = createTag("%IX0.0.13", false);
        bool myBit16 = createTag("%IX0.0.14", false);
        bool myBit17 = createTag("%IX0.0.15", false);
"""




type XgiTestClass() =
    inherit PLCGenerationTestClass()
    override x.GetCurrentRuntimeTarget() = XGI


