namespace UnitTest.Engine


open Engine.Core
open Engine.Common.FS
open NUnit.Framework
open System.Collections.Generic
open Engine.Parser.FS
open Model.Import.Office


[<AutoOpen>]
module pptTestModule =
    ///ppt로 부터 만든 모델을 text로 다시 읽어 이중 확인
    let check (model:Model, viewNodes:ViewNode seq) =
        let modelText =  model.TheSystem.Value.ToDsText()
        let helper = ModelParser.ParseFromString2(modelText, ParserOptions.Create4Runtime("localhost"))
        let reGenerated = helper.TheSystem.ToDsText()
        modelText =~= reGenerated

    type PPTTest() =
        do Fixtures.SetUpTest()

        [<Test>] member __.``CaseAll CaseAll test``       () = check (ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\T0_CaseAll.pptx"))
        [<Test>] member __.``System System test``         () = check (ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\T1_System.pptx"))
        [<Test>] member __.``Flow Flow test``             () = check (ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\T2_Flow.pptx"))
        [<Test>] member __.``Real Real test``             () = check (ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\T3_Real.pptx"))
        [<Test>] member __.``Api Api test``               () = check (ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\T4_Api.pptx"))
        [<Test>] member __.``Call Call test``             () = check (ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\T5_Call.pptx"))
        [<Test>] member __.``Alias Alias test``           () = check (ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\T6_Alias.pptx"))
        [<Test>] member __.``CopySystem CopySystem test`` () = check (ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\T7_CopySystem.pptx"))
        [<Test>] member __.``Safety test``                () = check (ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\T8_Safety.pptx"))
        [<Test>] member __.``Group test``                 () = check (ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\T9_Group.pptx"))
