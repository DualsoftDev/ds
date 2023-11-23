namespace T
open Dual.UnitTest.Common.FS

open Engine.Core
open Engine.Parser.FS
open NUnit.Framework
open System.Collections.Generic
open System.IO

[<AutoOpen>]
module OriginTestModule =
    type OriginTester() =
        inherit EngineTestBaseClass()

        let libdir = @$"{__SOURCE_DIRECTORY__}/../../UnitTest.Model"
            
        let configFile = PathManager.getFullPath  (@"test-origin-config.json"|>DsFile) (libdir.ToDirectory())
        let genConfig (filePath:string) =
            let cfg = {
                    DsFilePath = $@"{libdir}/MultipleJobdefCallExample/{filePath}"
                }
            ModelLoader.SaveConfig configFile cfg

        let answerChecker
                (filePath:string)
                (answer:seq<KeyValuePair<string, InitialType>>) =
            genConfig(filePath)
            let model = ParserLoader.LoadFromConfig(configFile)
            let originChecker =
                [
                        for f in model.System.Flows do
                            for v in f.Graph.Vertices do
                                match v with
                                | :? Real as r -> OriginHelper.GetOriginInfo r
                                | _ -> ()
                ]
                |> Seq.collect (fun f-> f.Tasks |> Seq.map(fun (d, t)->d.QualifiedName, t)) |> dict

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
                    KeyValuePair("S101_Copy1.Func5", On);
                    KeyValuePair("S101_Copy2.Func1", On);
                    KeyValuePair("S101_Copy2.Func2", NeedCheck);
                    KeyValuePair("S101_Copy1.Func1", Off);
                    KeyValuePair("S101_Copy1.Func2", On);
                    KeyValuePair("S101_Copy1.Func6", NotCare);
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
                    KeyValuePair("S101_Copy1.Func1", NotCare);
                    KeyValuePair("S101_Copy1.Func2", NotCare);
                    KeyValuePair("S101_Copy1.Func3", Off);
                    KeyValuePair("S101_Copy1.Func6", NotCare);
                    KeyValuePair("S101_Copy1.Func5", On);
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

     