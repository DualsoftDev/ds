namespace T.PPT
open Dual.Common.UnitTest.FS

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
    let pptParms:PptParams = defaultPptParams()
    let modelConfig = createDefaultModelConfig()    

    type PptTest() =
        inherit EngineTestBaseClass()

        

        [<Test>] member __.``System  test``    () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T1System.pptx",false, pptParms))
        [<Test>] member __.``Flow  test``      () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T2Flow.pptx",false, pptParms))
        [<Test>] member __.``Real  test``      () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T3Real.pptx",false, pptParms))
        [<Test>] member __.``Api  test``       () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T4Api.pptx",false, pptParms))
        [<Test>] member __.``Calltest``        () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T5Call.pptx",false, pptParms))
        [<Test>] member __.``Alias  test``     () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T6Alias.pptx",false, pptParms))
        [<Test>] member __.``CopySystem test`` () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T7CopySystem.pptx",false, pptParms))
        [<Test>] member __.``Safety test``     () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T8Safety.pptx",false, pptParms))
        [<Test>] member __.``Group test``      () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T9Group.pptx",false, pptParms))
        [<Test>] member __.``Button test``     () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T10Button.pptx",false, pptParms))
        [<Test>] member __.``SubLoading test`` () = check (ImportPpt.GetDSFromPptWithLib ($"{testpptPath}/T11SubLoading.pptx",false, pptParms))
