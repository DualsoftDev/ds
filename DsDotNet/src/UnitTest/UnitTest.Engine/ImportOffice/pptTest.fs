namespace T.PPT
open Dual.UnitTest.Common.FS

open T

open Engine.Core
open Dual.Common.Core.FS
open NUnit.Framework
open Engine.Parser.FS
open Engine.Import.Office


[<AutoOpen>]
module pptTestModule =

    ///ppt로 부터 만든 모델을 text로 다시 읽어 이중 확인
    let check (model:DSFromPPT) =
        let systemRepo = ShareableSystemRepository()
        let dsText =  model.System.ToDsText(true)
        let libdir = @$"{__SOURCE_DIRECTORY__}/../../UnitTest.Model/ImportOfficeExample/ppt/"
        let helper = ModelParser.ParseFromString2(dsText, ParserOptions.Create4Runtime(systemRepo, libdir, "localhost", None, DuNone, false, true))
        let reGenerated = helper.TheSystem.ToDsText(true)
        reGenerated.Length =!= 0 //파싱 확인만 text 비교는 순서바뀌어서 불가능
    let testpptPath = @$"{__SOURCE_DIRECTORY__}/../../UnitTest.Model/ImportOfficeExample/ppt/"
    let pptParms:PPTParams = {TargetType = WINDOWS; AutoIOM = true; CreateFromPPT = false; CreateBtnLamp = true}

    type PPTTest() =
        inherit EngineTestBaseClass()

        do
            RuntimeDS.Package <- PCSIM

        [<Test>] member __.``System  test``    () = check (ImportPPT.GetDSFromPPTWithLib ($"{testpptPath}/T1_System.pptx",false, pptParms))
        [<Test>] member __.``Flow  test``      () = check (ImportPPT.GetDSFromPPTWithLib ($"{testpptPath}/T2_Flow.pptx",false, pptParms))
        [<Test>] member __.``Real  test``      () = check (ImportPPT.GetDSFromPPTWithLib ($"{testpptPath}/T3_Real.pptx",false, pptParms))
        [<Test>] member __.``Api  test``       () = check (ImportPPT.GetDSFromPPTWithLib ($"{testpptPath}/T4_Api.pptx",false, pptParms))
        [<Test>] member __.``Calltest``        () = check (ImportPPT.GetDSFromPPTWithLib ($"{testpptPath}/T5_Call.pptx",false, pptParms))
        [<Test>] member __.``Alias  test``     () = check (ImportPPT.GetDSFromPPTWithLib ($"{testpptPath}/T6_Alias.pptx",false, pptParms))
        [<Test>] member __.``CopySystem test`` () = check (ImportPPT.GetDSFromPPTWithLib ($"{testpptPath}/T7_CopySystem.pptx",false, pptParms))
        [<Test>] member __.``Safety test``     () = check (ImportPPT.GetDSFromPPTWithLib ($"{testpptPath}/T8_Safety.pptx",false, pptParms)) 
        [<Test>] member __.``Group test``      () = check (ImportPPT.GetDSFromPPTWithLib ($"{testpptPath}/T9_Group.pptx",false, pptParms))
        [<Test>] member __.``Button test``     () = check (ImportPPT.GetDSFromPPTWithLib ($"{testpptPath}/T10_Button.pptx",false, pptParms))
        [<Test>] member __.``SubLoading test`` () = check (ImportPPT.GetDSFromPPTWithLib ($"{testpptPath}/T11_SubLoading.pptx",false, pptParms))
