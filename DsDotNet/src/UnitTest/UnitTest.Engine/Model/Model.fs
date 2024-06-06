namespace T
open Dual.UnitTest.Common.FS


open System.Linq
open Engine
open Engine.Core
open Dual.Common.Core.FS
open NUnit.Framework
open Engine.Parser.FS

[<AutoOpen>]
module private ModelComparisonHelper =
    let parseText (systemRepo:ShareableSystemRepository) referenceDir text =
        let helper = ModelParser.ParseFromString2(text, ParserOptions.Create4Simulation(systemRepo, referenceDir, "ActiveCpuName", None, DuNone))
        helper.TheSystem

    let compare (systemRepo:ShareableSystemRepository) referenceDir originalText answer =
        let system = parseText systemRepo referenceDir originalText
        validateGraphOfSystem system

        let generated = system.ToDsText(true);
        generated =~= answer


[<AutoOpen>]
module ModelTests1 =
    type DemoTests1() =
        inherit EngineTestBaseClass()

        let systemRepo = ShareableSystemRepository()
        let compare = compare systemRepo @$"{__SOURCE_DIRECTORY__}/../../UnitTest.Model/UnitTestExample/dsSimple"
        let compareExact x = compare x x
        [<Test>]
        member __.``0 Any temporary test`` () =
            logInfo "=== 0 Any test"
            let input = """


[sys] EX = {
    [flow] F = {
        TX, "R.X", "NameWith\"Quote"; // island
    }
    //[interfaces] = {
    //    "이상한. Api" = { F.TX ~ F."R.X" }
    //    "Dummy. Api" = { _ ~ _ }
    //}
}
"""
            compare input input

        [<Test>]
        member __.``CylinderText test`` () =
            logInfo "=== CylinderText"
            compare Program.CylinderText answerCylinderText

        [<Test>]
        member __.``EveryScenarioText test`` () =
            logInfo "=== EveryScenarioText"
            compare Program.EveryScenarioText answerEveryScenarioText

        //[<Test>] //사용안함
        //member __.``CodeElementsText test`` () =
        //    logInfo "=== CodeElementsText"
        //    compareExact Program.CodeElementsText

        [<Test>]
        member __.``CausalsText test`` () =
            logInfo "=== CausalsText"
            compareExact Program.CausalsText 

        [<Test>]
        member __.``AdoptoedValidText test`` () =
            logInfo "=== AdoptoedValidText"
            compareExact Program.AdoptoedValidText

        [<Test>]
        member __.``DuplicatedEdgesText test`` () =
            logInfo "=== DuplicatedEdgesText"
            compare Program.DuplicatedEdgesText answerDuplicatedEdgesText

        [<Test>]
        member __.``DuplicatedCallsText test`` () =
            logInfo "=== DuplicatedCallsText"
            compareExact Program.DuplicatedCallsText 

        [<Test>]
        member __.``SplittedMRIEdgesText test`` () =
            logInfo "=== SplittedMRIEdgesText"
            compare Program.SplittedMRIEdgesText answerSplittedMRIEdgesText

        [<Test>]
        member __.``SimpleLoadedDeviceText test`` () =
            logInfo "=== SimpleLoadedDeviceText"
            compareExact Program.SimpleLoadedDeviceText

        [<Test>]
        member __.``Model component [SafetyValid] test`` () =
            compare ParserTest.SafetyValid answerSafetyValid

        [<Test>]
        member __.``Model component [LayoutValid] test`` () =
            compare ParserTest.LayoutValid answerLayoutValid

        [<Test>]
        member __.``Model component [FinishValid] test`` () =
            compare ParserTest.FinishValid answerFinishValid

        [<Test>]
        member __.``Model component [DisableValid] test`` () =
            compare ParserTest.DisableValid answerDisableValid

   
        [<Test>]
        member __.``Model component [Buttons] test`` () =
            compare ParserTest.Buttons answerButtons

        [<Test>]
        member __.``Model component [Lamps] test`` () =
            compare ParserTest.Lamps answerLamps

        [<Test>]
        member __.``Model component [Conditions] test`` () =
            compare ParserTest.Conditions answerConditions

        [<Test>]
        member __.``Model component [Dup] test`` () =
            compareExact ParserTest.Dup

        [<Test>]
        member __.``Model component [Aliases] test`` () =
            compareExact ParserTest.Aliases

        [<Test>]
        member __.``Model component [Link and link aliases] test`` () =
            compareExact ParserTest.LinkAndLinkAliases 

        [<Test>]
        member __.``Model component [QualifiedName] test`` () =
            compareExact ParserTest.QualifiedName

        [<Test>]
        member __.``Model component [T6 alias] test`` () =
            compare ParserTest.T6Alias  answerT6Alias

        [<Test>]
        member __.``Model component [external circular dependency] test`` () =
            compare ParserTest.CircularDependency answerCircularDependency

        [<Test>]
        member __.``Model component [Task Device/Link (old:JobDef)] test`` () =
            compareExact ParserTest.TaskLinkorDevice 

        [<Test>]
        member __.``Model component [Function/Commands] test`` () =
            compareExact ParserTest.Commnads    
        [<Test>]
        member __.``Model component [Function/Operators] test`` () =
            compareExact ParserTest.Operators 
        [<Test>]
        member __.``Model component [DevParam] test`` () =
            compareExact ParserTest.DevParam 
            
        //[<Test>]
        //member __.``X Ppt20221213Text test`` () =
        //    // 현재 test 실패
        //    compareExact Program.Ppt20221213Text

        //[<Test>]
        //member __.``Model component test`` () =
        //    compare ParserTest.Ppt);
        //    compare ParserTest.ExternalSegmentCall ""
        //    compare ParserTest.ExternalSegmentCallConfusing ""
        //    compare ParserTest.MyFlowReference ""
        //    compare ParserTest.Error ""
        //    ()

        [<Test>]
        member __.``Model ERROR duplication test`` () =
            //(fun () -> compare InvalidDuplicationTest.DupSystemNameModel "") |> ShouldFailWithSubstringT "An item with the same key has already been added"
            //(fun () -> compare InvalidDuplicationTest.DupFlowNameModel "")   |> ShouldFailWithSubstringT "Duplicated"
            //(fun () -> compare InvalidDuplicationTest.DupParentingModel1 "") |> ShouldFailWithSubstringT "Duplicated"
            //(fun () -> compare InvalidDuplicationTest.DupParentingModel2 "") |> ShouldFailWithSubstringT "Duplicated"
            (fun () -> compare InvalidDuplicationTest.CyclicEdgeModel ""  )  |> ShouldFailWithSubstringT "Cyclic"


    type ModelGraphTests1() =
        inherit EngineTestBaseClass()

        let systemRepo = ShareableSystemRepository()
        let compare = compare systemRepo @$"{__SOURCE_DIRECTORY__}/../../UnitTest.Model"

        [<Test>]
        member __.``RecursiveSystem test`` () =
            logInfo "=== RecursiveSystem"
            (fun () -> compare Program.RecursiveSystemText "" ) |> ShouldFail

        [<Test>]
        member __.``Interlock extract simple test`` () =
            logInfo "=== Interlock extract"
            let testCode = """
            [sys] DoubleCylinder = {
                [flow] FLOW = {
                    ADV <|> RET;
                }
                [interfaces] = {
                    "+" = { FLOW.ADV ~ FLOW.ADV }
                    "-" = { FLOW.RET ~ FLOW.RET }
                }
            }
            """

            let systemRepo = ShareableSystemRepository()
            let referenceDir = $"{__SOURCE_DIRECTORY__}/../../UnitTest.Model/UnitTestExample/dsSimple"
            let helper = ModelParser.ParseFromString2(testCode, ParserOptions.Create4Simulation(systemRepo, referenceDir, "ActiveCpuName", None, DuNone))
            //"+" |> "-", "-" |> "+";
            helper.TheSystem.ApiResetInfos.Count === 2

        [<Test>]
        member __.``Interlock extract test`` () =
            logInfo "=== Interlock extract"
            let testCode = """
            [sys] DoubleCylinder = {
                [flow] FLOW = {
                    VP > PP > SP;
                    VM > PM > SM;
                    VP <|> VM;
                    PP <|> PM;
                    PP |> SM;
                    PM |> SP;
                }
                [interfaces] = {
                    ADV = { FLOW.VP ~ FLOW.VP }
                    RET = { FLOW.VM ~ FLOW.SM }
                    ADV <|> RET;
                }
            }
            """

            let systemRepo = ShareableSystemRepository()
            let referenceDir = $"{__SOURCE_DIRECTORY__}/../../UnitTest.Model/UnitTestExample/dsSimple"
            let helper = ModelParser.ParseFromString2(testCode, ParserOptions.Create4Simulation(systemRepo, referenceDir, "ActiveCpuName", None, DuNone))
            //ADV <|> RET, ADV |> RET, RET |> ADV;  => 기존 인과 존재하면 추가 안함
            helper.TheSystem.ApiResetInfos.Count === 1


        [<Test>]
        member __.``Interlock extract external Flow test`` () =
            logInfo "=== Interlock extract"
            let testCode = """
            [sys] DoubleCylinder = {
                [flow] FLOW = {
                    ADV > RET;
                    ADV <|> FLOWEx.RET;
                }
                [flow] FLOWEx = {
                    RET;
                }
                [interfaces] = {
                    "+" = { FLOW.ADV ~ FLOW.ADV }
                    "-" = { FLOWEx.RET ~ FLOWEx.RET }
                }
            }
            """

            let systemRepo = ShareableSystemRepository()
            let referenceDir = $"{__SOURCE_DIRECTORY__}/../../UnitTest.Model/UnitTestExample/dsSimple"
            let helper = ModelParser.ParseFromString2(testCode, ParserOptions.Create4Simulation(systemRepo, referenceDir, "ActiveCpuName", None, DuNone))


            helper.TheSystem.ApiResetInfos.Count === 2

        [<Test>]
        member __.``Interlock extract alias test`` () =
            logInfo "=== Interlock extract"
            let testCode = """
            [sys] DoubleCylinder = {
                
                [flow] F1 = {
                    R1 > R3;
                    R2 > R3;
                    R3 > ExR2;
                    [aliases] = {
                        F2.R2 = { ExR2; }
                    }
                }
                [flow] F2 = {
                    R2 > Copy1_R3;
                    R1 > R3;
                    R3 > F1.R1;
                    F1.R3 <|> R3;
                    [aliases] = {
                        R3 = { Copy1_R3; }
                    }
                }
                
                [interfaces] = {
                    "+" = { F1.R1 ~ F1.R3 }
                    "-" = { F2.R1 ~ F2.R3 }
                }
            }
            """

            let systemRepo = ShareableSystemRepository()
            let referenceDir = $"{__SOURCE_DIRECTORY__}/../../UnitTest.Model/UnitTestExample/dsSimple"
            let helper = ModelParser.ParseFromString2(testCode, ParserOptions.Create4Simulation(systemRepo, referenceDir, "ActiveCpuName", None, DuNone))


            helper.TheSystem.ApiResetInfos.Count === 2
  