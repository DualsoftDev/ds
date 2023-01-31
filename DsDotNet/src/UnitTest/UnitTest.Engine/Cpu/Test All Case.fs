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

    let t = CpuTestSample()
    let myTemplate testName = Path.Combine($"{__SOURCE_DIRECTORY__}", $"../../UnitTest.PLC.Xgi/XgiXmls/{testName}")

    [<Test>]
    member __.``XXXXXXXXXXXXXX Test All Case`` () =
        let f = get_current_function_name()
        let result = exportXMLforXGI(t.Sys, myTemplate f, None)
        //추후 정답과 비교 필요
        result === result


    [<Test>]
    member __.``XX ppt Model Cpu test``    () =
        let f = get_current_function_name()
        let sampleDirectory = Path.Combine($"{__SOURCE_DIRECTORY__}", "../ImportOffice/sample/");
        let pptPath = sampleDirectory + "s.pptx"
        let xlsPath = sampleDirectory + "s.xlsx"
        let model = ImportPPT.GetModel [ pptPath ]

        ApplyExcel(xlsPath, model.Systems)

        let result = exportXMLforXGI(model.Systems.First(), myTemplate f, None)
        //추후 정답과 비교 필요
        result === result
