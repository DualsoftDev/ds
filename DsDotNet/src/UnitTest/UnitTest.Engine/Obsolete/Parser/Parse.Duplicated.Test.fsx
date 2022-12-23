namespace T


open Engine
open NUnit.Framework

[<AutoOpen>]
module ParseDuplicatedTest =
    type DemoTests2() =
        do Fixtures.SetUpTest()

        let checkDuplicate(text:string) =
            (fun () -> InvalidDuplicationTest.Test(text)) |> ShouldFailWithSubstringT("Duplicated")


        [<Test>]
        member __.``Dup SystemName Model`` () =
            checkDuplicate(InvalidDuplicationTest.DupSystemNameModel)

        [<Test>]
        member __.``DupFlowNameModel`` () =
            checkDuplicate(InvalidDuplicationTest.DupFlowNameModel)

        [<Test>]
        member __.``DupParentingModel1`` () =
            checkDuplicate(InvalidDuplicationTest.DupParentingModel1)

        [<Test>]
        member __.``DupParentingModel2`` () =
            checkDuplicate(InvalidDuplicationTest.DupParentingModel2)

        [<Test>]
        member __.``DupParentingModel3`` () =
            checkDuplicate(InvalidDuplicationTest.DupParentingModel3)

        [<Test>]
        member __.``DupCallPrototypeModel`` () =
            checkDuplicate(InvalidDuplicationTest.DupCallPrototypeModel)

        [<Test>]
        member __.``DupParentingWithCallPrototypeModel`` () =
            checkDuplicate(InvalidDuplicationTest.DupParentingWithCallPrototypeModel)

        [<Test>]
        member __.``DupCallTxModel`` () =
            checkDuplicate(InvalidDuplicationTest.DupCallTxModel)

        [<Test>]
        member __.``DupButtonCategory`` () =
            checkDuplicate(InvalidDuplicationTest.DupButtonCategory)

        [<Test>]
        member __.``DupButtonName`` () =
            checkDuplicate(InvalidDuplicationTest.DupButtonName)

        [<Test>]
        member __.``DupButtonFlowName`` () =
            checkDuplicate(InvalidDuplicationTest.DupButtonFlowName)
