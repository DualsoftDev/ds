namespace T

open System.IO
open Engine.Core
open NUnit.Framework
open Engine.CodeGenHMI

[<AutoOpen>]
module HmiCodeGenTestModule =
    type HmiCodeGenTester() =
        inherit EngineTestBaseClass()

        let libdir = @$"{__SOURCE_DIRECTORY__}\..\Libraries"
        let configFile = @"test-hmi-gen-config.json"
        let genConfig (filePaths:string list) =
            let cfg = {
                    DsFilePaths = [
                        for path in filePaths do
                            $@"{libdir}\HmiCodeGenExample\{path}"
                    ]
                }
            ModelLoader.SaveConfig configFile cfg
        let answerFile (answerPath:string) =
            $@"{libdir}\HmiCodeGenExample\{answerPath}"
        let answerChecker (filePaths:string list) (answerPath:string) =
            genConfig (filePaths)
            let asw = answerFile(answerPath)
            let codeGenHandler = CodeGenHandler.ParseModel(configFile)
            let result = codeGenHandler.SelectedResult "ds-hmi"
            use stream = new StreamReader(asw)
            let answer = stream.ReadLine()
            ShouldEqual result answer

        [<Test>]
        member __.``HmiTestCase-FactoryIO`` () =
            answerChecker
                [
                    "FactoryIO\FactoryIO.ds";
                    "FactoryIO\Main.ds";
                    "FactoryIO\Assy.ds";
                ]
                "answer_factoryIO.json"

        [<Test>]
        member __.``HmiTestCase-test_sample`` () =
            answerChecker
                [
                    "test_sample\control.ds";
                    "test_sample\device\MovingLifter1.ds";
                    "test_sample\device\MovingLifter2.ds";
                ]
                "answer_test_sample.json"