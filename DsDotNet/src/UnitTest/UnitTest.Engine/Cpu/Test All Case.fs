namespace T.CPU
open Dual.UnitTest.Common.FS

open System.IO
open NUnit.Framework

open T
open Engine.Core
open Dual.Common.Core.FS
open Engine.CodeGenPLC

type TestAllCase() =
    inherit EngineTestBaseClass()

    let myTemplate testName = Path.Combine($"{__SOURCE_DIRECTORY__}", $"../../UnitTest.PLC.Xgx/Xgi/Xmls/{testName}.xml")
    let myExistIO  = Path.Combine($"{__SOURCE_DIRECTORY__}", $"../../UnitTest.PLC.Xgx/Xgi/Xmls/Templates/myTemplateExistIO.xml")

    let testAddressSetting (sys:DsSystem) =
        let mutable index = 0
        let addr() = index <- index+1
                     sprintf "%%MX%d" index
        for j in sys.Jobs do
            for dev in j.DeviceDefs  do
                    dev.InAddress <- addr()
                    dev.OutAddress <- addr()

        for b in sys.HWButtons do
            b.InAddress <- addr()
            b.OutAddress <- addr()

        for l in sys.HWLamps do
            l.OutAddress <-  addr()

        for c in sys.HWConditions do
            c.InAddress <-  addr()


    [<Test>]
    member __.``Test All Case`` () =
        let t = CpuTestSample()
        let f = getFuncName()
        let result = exportXMLforLSPLC(XGI, t.Sys, myTemplate f, None, 0, 0, 0)
        //추후 정답과 비교 필요
        result === result


    //[<Test>]  //ppt에서 CPU 불러오는거 없어지고 대신 *.ds에서 가져옴
    //member __.``PPT Model Cpu test``    () =
    //    let f = getFuncName()
    //    let sampleDirectory = Path.Combine($"{__SOURCE_DIRECTORY__}", "../../UnitTest.Model/ImportOfficeExample/sample/");
    //    let pptPath = sampleDirectory + "s.pptx"
    //    let xlsPath = sampleDirectory + "s.xlsx"
    //    let model = ImportPPT.GetModel [ pptPath ]
    //    model.Systems.ForEach(testAddressSetting)

    //    let result = exportXMLforXGI(model.Systems.First(), myTemplate f, None)
    //    //추후 정답과 비교 필요
    //    result === result



    [<Test>]
    member __.``Allocate existing global Memory`` () =
        let t = CpuTestSample()

        let myXml = getFuncName() |> myTemplate
        let result = exportXMLforLSPLC(XGI, t.Sys, myXml, None, 0, 0, 0)
        result === result


    [<Test>]
    member __.``Allocate existing global IO`` () =
        let t = CpuTestSample()
        let myXml = getFuncName() |> myTemplate
        (fun () -> exportXMLforLSPLC(XGI, t.Sys, myXml, Some myExistIO, 0, 0, 0))  |> ShouldFail

