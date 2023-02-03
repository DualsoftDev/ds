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


    [<Test>]
    member __.``help kwak XXXX 성능 개선중 Model Origin Cpu test``    () =
        //GetOriginsWithDeviceDefs (graph:DsGraph) 성능 개선중
        //<kwak> help
        //하나의 Real에 자식 Coin이 11ea 일 경우 기준 원위치 뽑는데 1초이상 걸리고
        //real 4개가 제 PC 기준 8초 정도 걸리고 있습니다.
        //시스템에 100개 real이 존재할경우... real 병렬처리 이전에 하나라도 좀 빨라지면
        //<kwak> help
        let f = getFuncName()
        let sampleDirectory = Path.Combine($"{__SOURCE_DIRECTORY__}", "../../UnitTest.Model/ImportOfficeExample/sample/");
        let pptPath = sampleDirectory + "s_car.pptx"
        let model = ImportPPT.GetModel [ pptPath ]
        model.Systems.ForEach(testAddressSetting)

        let result = exportXMLforXGI(model.Systems.First(), myTemplate f, None)
        //추후 정답과 비교 필요
        result === result

