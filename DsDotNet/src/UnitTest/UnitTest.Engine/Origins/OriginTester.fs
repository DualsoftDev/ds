namespace T
open Dual.Common.UnitTest.FS

open Engine.Core
open Engine.Parser.FS
open NUnit.Framework
open System.Collections.Generic
open System.IO
open System

[<AutoOpen>]
module OriginTestModule =
    type OriginTester() =
        inherit EngineTestBaseClass()

        let libdir = @$"{__SOURCE_DIRECTORY__}/../../UnitTest.Model"
            
        let configFile = PathManager.getFullPath  (@"modelConfig.json"|>DsFile) (libdir.ToDirectory())
        let genConfig (filePath:string) =
            let mConfig = createDefaultModelConfig()
            let cfg = createModelConfigReplacePath(mConfig, $@"{libdir}/MultipleJobdefCallExample/{filePath}" )
            SaveConfig configFile cfg

        let answerChecker
                (filePath:string)
                (answer:seq<KeyValuePair<string, InitialType>>) =
            genConfig(filePath)
            let model = ParserLoader.LoadFromConfig(configFile) HwCPU.WINDOWS 
            let originChecker = 
                [
                        for f in model.System.Flows do
                            for v in f.Graph.Vertices do
                                match v with
                                | :? Real as r -> yield! OriginHelper.GetOriginInfoByTaskName r
                                | _ -> ()
                ]
                |> dict

            for r in originChecker do
                Console.WriteLine($"org {r}")

            for a in answer do
                Console.WriteLine($"asw {a}")

            SeqEq originChecker answer

        [<Test>]
        member __.``OriginTestCase0`` () =
            let answer:seq<KeyValuePair<string, InitialType>> =
                seq {
                    KeyValuePair("T7_CopySystem.S101_Copy1.Func3", Off);
                    KeyValuePair("T7_CopySystem.S101_Copy1.Func4", NotCare);
                    KeyValuePair("T7_CopySystem.S101_Copy1.Func5", On);
                    KeyValuePair("T7_CopySystem.S101_Copy1.Func1", Off);
                    KeyValuePair("T7_CopySystem.S101_Copy1.Func2", On);
                    KeyValuePair("T7_CopySystem.S101_Copy1.Func6", NotCare);
                    KeyValuePair("T7_CopySystem.S102_SystemA1.Func1", Off);
                    KeyValuePair("T7_CopySystem.S102_SystemA2.Func1", Off);
                    KeyValuePair("T7_CopySystem.S102_SystemA3.Func1", Off);
                    KeyValuePair("T7_CopySystem.S102_SystemA4.Func1", Off);
                    KeyValuePair("T7_CopySystem.S102_SystemA5.Func1", Off);
                    KeyValuePair("T7_CopySystem.S102_SystemA1.Func2", On);
                    KeyValuePair("T7_CopySystem.S102_SystemA2.Func2", On);
                    KeyValuePair("T7_CopySystem.S102_SystemA3.Func2", On);
                    KeyValuePair("T7_CopySystem.S102_SystemA4.Func2", On);
                    KeyValuePair("T7_CopySystem.S102_SystemA5.Func2", On);
                }
            answerChecker "test_case_0.ds" answer

        [<Test>]
        member __.``OriginTestCase1`` () =
            let answer:seq<KeyValuePair<string, InitialType>> =
                seq {
                    KeyValuePair("T7_CopySystem.S101_Copy2.Func1", Off);
                    KeyValuePair("T7_CopySystem.S101_Copy2.Func2", On);
                    KeyValuePair("T7_CopySystem.S101_Copy1.Func1", NotCare);
                    KeyValuePair("T7_CopySystem.S101_Copy1.Func2", NotCare);
                    KeyValuePair("T7_CopySystem.S101_Copy1.Func3", Off);
                    KeyValuePair("T7_CopySystem.S101_Copy1.Func4", NotCare);
                    KeyValuePair("T7_CopySystem.S101_Copy1.Func6", NotCare);
                    KeyValuePair("T7_CopySystem.S101_Copy1.Func5", On);
                    KeyValuePair("T7_CopySystem.S102_SystemA1.Func1", Off);
                    KeyValuePair("T7_CopySystem.S102_SystemA2.Func1", Off);
                    KeyValuePair("T7_CopySystem.S102_SystemA3.Func1", Off);
                    KeyValuePair("T7_CopySystem.S102_SystemA4.Func1", Off);
                    KeyValuePair("T7_CopySystem.S102_SystemA5.Func1", Off);
                    KeyValuePair("T7_CopySystem.S102_SystemA1.Func2", On);
                    KeyValuePair("T7_CopySystem.S102_SystemA2.Func2", On);
                    KeyValuePair("T7_CopySystem.S102_SystemA3.Func2", On);
                    KeyValuePair("T7_CopySystem.S102_SystemA4.Func2", On);
                    KeyValuePair("T7_CopySystem.S102_SystemA5.Func2", On);
                }
            answerChecker "test_case_1.ds" answer

     