namespace T


open Engine.Core
open Engine.Common.FS
open NUnit.Framework
open System.Collections.Generic
open Engine.Parser.FS
open Model.Import.Office


[<AutoOpen>]
module pptTestModule =

    ///ppt로 부터 만든 모델을 text로 다시 읽어 이중 확인
    let check (model:Model) =
        let systemRepo = ShareableSystemRepository()
        model.Systems.ForEach(fun system->
            let dsText =  system.ToDsText()
            let libdir = @$"{__SOURCE_DIRECTORY__}\Sample\"
            let helper = ModelParser.ParseFromString2(dsText, ParserOptions.Create4Runtime(systemRepo, libdir, "localhost", None, DuNone))
            let reGenerated = helper.TheSystem.ToDsText()
            reGenerated.Length =!= 0 //파싱 확인만 text 비교는 순서바뀌어서 불가능
        )

    type PPTTest() =
        do Fixtures.SetUpTest()

        [<Test>] member __.``System  test``    () = check (ImportPPT.GetModel [ $"{__SOURCE_DIRECTORY__}\\ppt\\T1_System.pptx"])
        [<Test>] member __.``Flow  test``      () = check (ImportPPT.GetModel [ $"{__SOURCE_DIRECTORY__}\\ppt\\T2_Flow.pptx"])
        [<Test>] member __.``Real  test``      () = check (ImportPPT.GetModel [ $"{__SOURCE_DIRECTORY__}\\ppt\\T3_Real.pptx"])
        [<Test>] member __.``Api  test``       () = check (ImportPPT.GetModel [ $"{__SOURCE_DIRECTORY__}\\ppt\\T4_Api.pptx"])
        [<Test>] member __.``Call  test``      () = check (ImportPPT.GetModel [ $"{__SOURCE_DIRECTORY__}\\ppt\\T5_Call.pptx"])
        [<Test>] member __.``Alias  test``     () = check (ImportPPT.GetModel [ $"{__SOURCE_DIRECTORY__}\\ppt\\T6_Alias.pptx"])
        [<Test>] member __.``CopySystem test`` () = check (ImportPPT.GetModel [ $"{__SOURCE_DIRECTORY__}\\ppt\\T7_CopySystem.pptx"])
        [<Test>] member __.``Safety test``     () = check (ImportPPT.GetModel [ $"{__SOURCE_DIRECTORY__}\\ppt\\T8_Safety.pptx"])
        [<Test>] member __.``Group test``      () = check (ImportPPT.GetModel [ $"{__SOURCE_DIRECTORY__}\\ppt\\T9_Group.pptx"])
        [<Test>] member __.``Button test``     () = check (ImportPPT.GetModel [ $"{__SOURCE_DIRECTORY__}\\ppt\\T10_Button.pptx"])
        [<Test>] member __.``SubLoading test`` () = check (ImportPPT.GetModel [ $"{__SOURCE_DIRECTORY__}\\ppt\\T11_SubLoading.pptx"])
