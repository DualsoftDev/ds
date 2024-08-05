namespace T.PPT
open Dual.UnitTest.Common.FS

open T

open Engine.Core
open Dual.Common.Core.FS
open NUnit.Framework
open Engine.Parser.FS
open Engine.Import.Office


[<AutoOpen>]
module PptTestModule =

    ///ppt로 부터 만든 모델을 text로 다시 읽어 이중 확인
    let check (model:DSFromPpt) =
        let systemRepo = ShareableSystemRepository()
        let dsText =  model.System.ToDsText(true, false)
        let libdir = @$"{__SOURCE_DIRECTORY__}/../../UnitTest.Model/ImportOfficeExample/ppt/"
        let helperSys = ModelParser.ParseFromString(dsText, ParserOptions.Create4Runtime(systemRepo, libdir, "localhost", None, DuNone, false, true))
        let reGenerated = helperSys.ToDsText(true, false)
        reGenerated.Length =!= 0 //파싱 확인만 text 비교는 순서바뀌어서 불가능
    let testpptPath = @$"{__SOURCE_DIRECTORY__}/../../UnitTest.Model/ImportOfficeExample/ppt/"
    let pptParms:PptParams = {TargetType = WINDOWS; AutoIOM = true; CreateFromPpt = false; CreateBtnLamp = true}

    type PptTest() =
        inherit EngineTestBaseClass()

        do
            RuntimeDS.Package <- PCSIM

        [<Test>] member __.``System  test``    () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T1_System.pptx",false, pptParms))
        [<Test>] member __.``Flow  test``      () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T2_Flow.pptx",false, pptParms))
        [<Test>] member __.``Real  test``      () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T3_Real.pptx",false, pptParms))
        [<Test>] member __.``Api  test``       () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T4_Api.pptx",false, pptParms))
        [<Test>] member __.``Calltest``        () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T5_Call.pptx",false, pptParms))
        [<Test>] member __.``Alias  test``     () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T6_Alias.pptx",false, pptParms))
        [<Test>] member __.``CopySystem test`` () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T7_CopySystem.pptx",false, pptParms))
        [<Test>] member __.``Safety test``     () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T8_Safety.pptx",false, pptParms))
        [<Test>] member __.``Group test``      () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T9_Group.pptx",false, pptParms))
        [<Test>] member __.``Button test``     () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T10_Button.pptx",false, pptParms))
        [<Test>] member __.``SubLoading test`` () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T11_SubLoading.pptx",false, pptParms))
