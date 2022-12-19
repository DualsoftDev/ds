namespace UnitTest.Engine


open Engine.Core
open Engine.Common.FS
open NUnit.Framework
open System.Collections.Generic
open Engine.Parser.FS
open Model.Import.Office


[<AutoOpen>]
module xlsTestModule =
    ///ppt로 부터 만든 모델을 text로 다시 읽어 이중 확인
    let check (system:DsSystem, viewNodes:ViewNode seq) =
        let modelText =  system.ToDsText()
        let libdir = @$"{__SOURCE_DIRECTORY__}\\xls"
        let helper = ModelParser.ParseFromString2(modelText, ParserOptions.Create4Runtime(libdir, "localhost"))
        let reGenerated = helper.TheSystem.ToDsText()
        reGenerated.Length =!= 0 //파싱 확인만 text 비교는 순서바뀌어서 불가능

    type XLSTest() =
        do Fixtures.SetUpTest()
        let system, views = ImportM.FromPPTX($"{__SOURCE_DIRECTORY__}\\ppt\\T5_Call.pptx")
        //[<Test>] member __.``Command test``     () =  (ImportIOTable.ApplyExcel($"{__SOURCE_DIRECTORY__}\\xls\\Command.xlsx", system))
        //[<Test>] member __.``Observer test``    () =  (ImportIOTable.ApplyExcel($"{__SOURCE_DIRECTORY__}\\xls\\Observer.xlsx", system))
