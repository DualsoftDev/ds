namespace T
open Dual.UnitTest.Common.FS

open Engine.Core
open Engine.Parser.FS
open NUnit.Framework
open System.Collections.Generic

[<AutoOpen>]
module OriginTestModule =
    type OriginTester() =
        inherit EngineTestBaseClass()

        let libdir = @$"{__SOURCE_DIRECTORY__}/../../UnitTest.Model"
        let configFile = @"test-origin-config.json"
        let genConfig (filePath:string) =
            let cfg = {
                    DsFilePaths = [
                        $@"{libdir}/MultipleJobdefCallExample/{filePath}"
                    ]
                }
            ModelLoader.SaveConfig configFile cfg

        let answerChecker
                (filePath:string)
                (answer:seq<KeyValuePair<string, InitialType>>) =
            genConfig(filePath)
            let model = ModelLoader.LoadFromConfig(configFile)
            let originChecker =
                [
                    for sys in model.Systems do
                        for f in sys.Flows do
                            for v in f.Graph.Vertices do
                                match v with
                                | :? Real as r -> OriginHelper.GetOrigins r.Graph
                                | _ -> ()
                ]
                |> Seq.collect id

            for r in originChecker do
                printfn "org %A" r

            for a in answer do
                printfn "asw %A" a

            SeqEq originChecker answer

        [<Test>]
        member __.``OriginTestCase0`` () =
            let answer:seq<KeyValuePair<string, InitialType>> =
                seq {
                    KeyValuePair("S101_Copy1.Func3", Off);
                    KeyValuePair("S101_Copy1.Func4", NeedCheck);
                    KeyValuePair("S101_Copy1.Func5", NeedCheck);
                    KeyValuePair("S101_Copy2.Func1", Off);
                    KeyValuePair("S101_Copy2.Func2", On);
                    KeyValuePair("S101_Copy1.Func1", Off);
                    KeyValuePair("S101_Copy1.Func2", On);
                    KeyValuePair("S101_Copy1.Func3", Off);
                    KeyValuePair("S101_Copy1.Func4", NeedCheck);
                    KeyValuePair("S101_Copy1.Func6", Off);
                    KeyValuePair("S101_Copy1.Func5", NeedCheck);
                    KeyValuePair("S102_SystemA1.Func1", Off);
                    KeyValuePair("S102_SystemA2.Func1", Off);
                    KeyValuePair("S102_SystemA3.Func1", Off);
                    KeyValuePair("S102_SystemA4.Func1", Off);
                    KeyValuePair("S102_SystemA5.Func1", Off);
                    KeyValuePair("S102_SystemA1.Func2", On);
                    KeyValuePair("S102_SystemA2.Func2", On);
                    KeyValuePair("S102_SystemA3.Func2", On);
                    KeyValuePair("S102_SystemA4.Func2", On);
                    KeyValuePair("S102_SystemA5.Func2", On);
                }
            answerChecker "test_case_0.ds" answer

        [<Test>]
        member __.``OriginTestCase1`` () =
            let answer:seq<KeyValuePair<string, InitialType>> =
                seq {
                    KeyValuePair("S101_Copy2.Func1", Off);
                    KeyValuePair("S101_Copy2.Func2", On);
                    KeyValuePair("S101_Copy1.Func1", NeedCheck);
                    KeyValuePair("S101_Copy1.Func2", NeedCheck);
                    KeyValuePair("S101_Copy1.Func3", NotCare);
                    KeyValuePair("S101_Copy1.Func6", Off);
                    KeyValuePair("S101_Copy1.Func5", NotCare);
                    KeyValuePair("S102_SystemA1.Func1", Off);
                    KeyValuePair("S102_SystemA2.Func2", Off);
                    KeyValuePair("S102_SystemA3.Func1", Off);
                    KeyValuePair("S102_SystemA4.Func2", Off);
                    KeyValuePair("S102_SystemA5.Func1", Off);
                    KeyValuePair("S102_SystemA1.Func2", On);
                    KeyValuePair("S102_SystemA2.Func1", On);
                    KeyValuePair("S102_SystemA3.Func2", On);
                    KeyValuePair("S102_SystemA4.Func1", On);
                    KeyValuePair("S102_SystemA5.Func2", On);
                }
            answerChecker "test_case_1.ds" answer

        [<Test>]
        member __.``OriginTestCase2`` () =
            let answer:seq<KeyValuePair<string, InitialType>> =
                seq {
                    KeyValuePair("S101_Copy2.Func1", Off);
                    KeyValuePair("S101_Copy2.Func2", On);
                    KeyValuePair("S101_Copy1.Func1", NeedCheck);
                    KeyValuePair("S101_Copy1.Func2", NeedCheck);
                    KeyValuePair("S101_Copy1.Func3", Off);
                    KeyValuePair("S101_Copy1.Func4", NeedCheck);
                    KeyValuePair("S101_Copy1.Func6", NotCare);
                    KeyValuePair("S101_Copy1.Func5", NeedCheck);
                    KeyValuePair("S102_SystemA1.Func1", Off);
                    KeyValuePair("S102_SystemA2.Func1", Off);
                    KeyValuePair("S102_SystemA3.Func1", Off);
                    KeyValuePair("S102_SystemA4.Func1", Off);
                    KeyValuePair("S102_SystemA5.Func1", Off);
                    KeyValuePair("S102_SystemA1.Func2", On);
                    KeyValuePair("S102_SystemA2.Func2", On);
                    KeyValuePair("S102_SystemA3.Func2", On);
                    KeyValuePair("S102_SystemA4.Func2", On);
                    KeyValuePair("S102_SystemA5.Func2", On);
                }
            answerChecker "test_case_2.ds" answer

        [<Test>]
        member __.``OriginTestCase3`` () =
            let answer:seq<KeyValuePair<string, InitialType>> =
                seq {
                    KeyValuePair("S101_Copy2.Func2", Off);
                    KeyValuePair("S101_Copy2.Func1", On);
                    KeyValuePair("S101_Copy1.Func1", NotCare);
                    KeyValuePair("S101_Copy1.Func6", Off);
                    KeyValuePair("S101_Copy1.Func5", NotCare);
                    KeyValuePair("S101_Copy1.Func1", NeedCheck);
                    KeyValuePair("S101_Copy1.Func2", NeedCheck);
                    KeyValuePair("S101_Copy1.Func3", Off);
                    KeyValuePair("S101_Copy1.Func4", NeedCheck);
                    KeyValuePair("S101_Copy1.Func5", NeedCheck);
                    KeyValuePair("S102_SystemA1.Func1", Off);
                    KeyValuePair("S102_SystemA3.Func1", Off);
                    KeyValuePair("S102_SystemA5.Func1", Off);
                    KeyValuePair("S102_SystemA1.Func2", On);
                    KeyValuePair("S102_SystemA2.Func2", NotCare);
                    KeyValuePair("S102_SystemA3.Func2", On);
                    KeyValuePair("S102_SystemA4.Func2", NotCare);
                    KeyValuePair("S102_SystemA5.Func2", On);
                }
            answerChecker "test_case_3.ds" answer