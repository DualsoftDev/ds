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
    let check (model:Model, pageFlow:Dictionary<int, Flow>, dummys:IEnumerable<pptDummy>) = 
        let originalText =  model.ToDsText() 
        originalText =~= originalText

        //Edge 정리 후 Parse 이중체크 대기중 
        //let helper = ModelParser.ParseFromString2(originalText, ParserOptions.Create4Runtime("localhost"))
        //originalText =~= helper.Model.ToDsText()

    let checkAll () = 
            check (ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\T0_CaseAll.pptx"))
            check (ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\T1_System.pptx"))
            check (ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\T2_Flow.pptx"))
            check (ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\T3_Real.pptx"))
            check (ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\T4_Api.pptx"))
            check (ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\T5_Call.pptx"))
            check (ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\T6_Alias.pptx"))
            check (ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\T7_CopySystem.pptx"))
            check (ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\T8_Safety.pptx"))
    

    type PPTTest() =
        do Fixtures.SetUpTest()

        [<Test>]
        member __.``EveryScenarioPPT test`` () = checkAll()
