namespace T.CPU

open System.IO
open System.Linq
open NUnit.Framework

open T
open Engine.Core
open Engine.Common.FS
open Engine.CodeGenCPU
open PLC.CodeGen.LSXGI
open System
open Model.Import.Office

type TestAllCase() =
    inherit EngineTestBaseClass()

    let myTemplate testName = Path.Combine($"{__SOURCE_DIRECTORY__}", $"../../UnitTest.PLC.Xgi/XgiXmls/{testName}.xml")


    [<Test>]
    member __.``Test All Case`` () =
        let t = CpuTestSample()
        let f = getFuncName()
        let result = exportXMLforXGI(t.Sys, myTemplate f, None)
        //추후 정답과 비교 필요
        result === result


    [<Test>]
    member __.``PPT Model Cpu test``    () =
        let f = getFuncName()
        let sampleDirectory = Path.Combine($"{__SOURCE_DIRECTORY__}", "../../UnitTest.Model/ImportOfficeExample/sample/");
        let pptPath = sampleDirectory + "s.pptx"
        let xlsPath = sampleDirectory + "s.xlsx"
        let model = ImportPPT.GetModel [ pptPath ]
        model.Systems.ForEach(testAddressSetting)

        let result = exportXMLforXGI(model.Systems.First(), myTemplate f, None)
        //추후 정답과 비교 필요
        result === result


