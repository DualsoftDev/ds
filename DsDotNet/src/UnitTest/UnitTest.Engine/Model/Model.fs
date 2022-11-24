namespace UnitTest.Engine


open System.Linq
open Engine
open Engine.Core
open Engine.Common.FS
open NUnit.Framework
open Engine.Parser.FS
open System.Text.RegularExpressions

[<AutoOpen>]
module private ModelComparisonHelper =
    let (=~=) (xs:string) (ys:string) =
        let removeComment input =
            let blockComments = @"/\*(.*?)\*/"
            let lineComments = @"//(.*?)$"
            Regex.Replace(input, $"{blockComments}|{lineComments}", "", RegexOptions.Singleline)

        let toArray (xs:string) =
            xs.SplitByLine()
                .Select(removeComment)
                .Select(fun x -> x.Trim())
                |> Seq.where(fun x -> x.Any() && not <| x.StartsWith("//"))
                |> Array.ofSeq
        let xs = toArray xs
        let ys = toArray ys
        for (x, y) in Seq.zip xs ys do
            if x.Trim() <> y.Trim() then
                failwithf "[%s] <> [%s]" x y
        xs.Length === ys.Length



    let compare referenceDir originalText answer =
        let helper = ModelParser.ParseFromString2(originalText, ParserOptions.Create4Simulation(referenceDir, "ActiveCpuName"))
        let system = helper.TheSystem

        validateGraphOfSystem system

        let generated = system.ToDsText();
        generated =~= answer


[<AutoOpen>]
module ModelTests1 =
    type DemoTests1() =
        do
            Fixtures.SetUpTest()

        let compare = compare @$"{__SOURCE_DIRECTORY__}\..\Libraries"
        let compareExact x = compare x x
        [<Test>]
        member __.``0 Any temporary test`` () =
            logInfo "=== 0 Any test"
            let input = """
[sys] EX = {
    [flow] F = {
        TX;
        "R.X";
        "NameWith\"Quote";
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

        [<Test>]
        member __.``CodeElementsText test`` () =
            logInfo "=== CodeElementsText"
            compareExact Program.CodeElementsText

        [<Test>]
        member __.``CausalsText test`` () =
            logInfo "=== CausalsText"
            compare Program.CausalsText answerCausalsText

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
            compare Program.DuplicatedCallsText answerDuplicatedCallsText

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
            compareExact ParserTest.SafetyValid

        [<Test>]
        member __.``Model component [StrongCausal] test`` () =
            compare ParserTest.StrongCausal answerStrongCausal

        [<Test>]
        member __.``Model component [Buttons] test`` () =
            compare ParserTest.Buttons answerButtons

        [<Test>]
        member __.``Model component [Dup] test`` () =
            compareExact ParserTest.Dup

        [<Test>]
        member __.``Model component [Aliases] test`` () =
            compareExact ParserTest.Aliases

        [<Test>]
        member __.``Model component [QualifiedName] test`` () =
            compareExact ParserTest.QualifiedName

        [<Test>]
        member __.``Model component [T6 alias] test`` () =
            compare ParserTest.T6Alias answerT6Aliases

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


    type InvalidModelTests1() =
        do
            Fixtures.SetUpTest()

        let compare = compare @$"{__SOURCE_DIRECTORY__}\..\Libraries"

        [<Test>]
        member __.``RecursiveSystem test`` () =
            logInfo "=== RecursiveSystem"
            (fun () -> compare Program.RecursiveSystemText "" ) |> ShouldFail
