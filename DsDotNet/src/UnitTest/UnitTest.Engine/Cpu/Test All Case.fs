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
    let myTemplate testName = Path.Combine($"{__SOURCE_DIRECTORY__}", $"../../UnitTest.PLC.Xgi/XgiXmls/{testName}.xml")
    let testAddressSetting (sys:DsSystem) =
        for j in sys.Jobs do
            for dev in j.DeviceDefs do
            if dev.ApiItem.RXs.any() then  dev.InAddress <- "%MX777"
            if dev.ApiItem.TXs.any() then  dev.OutAddress <- "%MX888"

        for b in sys.Buttons do
            b.InAddress <- "%MX777"
            b.OutAddress <- "%MX888"

        for l in sys.Lamps do
            l.OutAddress <- "%MX888"

        for c in sys.Conditions do
            c.InAddress <- "%MX777"


    [<Test>]
    member __.``Test All Case`` () =
        let f = get_current_function_name()
        let result = exportXMLforXGI(t.Sys, myTemplate f, None)
        //추후 정답과 비교 필요
        result === result


    [<Test>]
    member __.``PPT Model Cpu test``    () =
        let f = get_current_function_name()
        let sampleDirectory = Path.Combine($"{__SOURCE_DIRECTORY__}", "../ImportOffice/sample/");
        let pptPath = sampleDirectory + "s.pptx"
        let xlsPath = sampleDirectory + "s.xlsx"
        let model = ImportPPT.GetModel [ pptPath ]
        model.Systems.ForEach(testAddressSetting)
        t.GenerationIO()

        let result = exportXMLforXGI(model.Systems.First(), myTemplate f, None)
        //추후 정답과 비교 필요
        result === result
