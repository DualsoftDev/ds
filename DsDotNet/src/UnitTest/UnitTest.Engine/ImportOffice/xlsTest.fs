namespace T
open Dual.UnitTest.Common.FS


open Engine.Core
open Dual.Common.Core.FS
open Engine.Parser.FS
open Engine.Import.Office


[<AutoOpen>]
module xlsTestModule =
    ///ppt로 부터 만든 모델을 text로 다시 읽어 이중 확인
    let check (model:Model) =
        let systemRepo = ShareableSystemRepository()
        model.Systems.ForEach(fun system->
            let dsText =  system.ToDsText(true)
            let libdir = @$"{__SOURCE_DIRECTORY__}/Sample/"
            let helper = ModelParser.ParseFromString2(dsText, ParserOptions.Create4Runtime(systemRepo, libdir, "localhost", None, DuNone))
            let reGenerated = helper.TheSystem.ToDsText(true)
            reGenerated.Length =!= 0 //파싱 확인만 text 비교는 순서바뀌어서 불가능
        )

    type XLSTest() =
        inherit EngineTestBaseClass()
        let systemRepo = ShareableSystemRepository()
        let model = ImportPPT.GetModel [$"{__SOURCE_DIRECTORY__}/ppt/T5_Call.pptx"]
        //[<Test>] member __.``Command test``     () =  (ImportIOTable.ApplyExcel($"{__SOURCE_DIRECTORY__}/xls/Command.xlsx", system))
        //[<Test>] member __.``Observer test``    () =  (ImportIOTable.ApplyExcel($"{__SOURCE_DIRECTORY__}/xls/Observer.xlsx", system))
