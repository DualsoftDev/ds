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
    let myExistIO  = Path.Combine($"{__SOURCE_DIRECTORY__}", $"../../UnitTest.PLC.Xgi/XgiXmls/Templates/myTemplateExistIO.xml")

    let testAddressSetting (sys:DsSystem) =
        let mutable index = 0
        let addr() = index <- index+1
                     sprintf "%%MX%d" index
        for j in sys.Jobs do
            for dev in j.DeviceDefs  do
                if dev.ApiItem.RXs.any() then
                    dev.InAddress <- addr()
                if dev.ApiItem.TXs.any() then
                    dev.OutAddress <- addr()

        for b in sys.Buttons do
            b.InAddress <- addr()
            b.OutAddress <- addr()

        for l in sys.Lamps do
            l.OutAddress <-  addr()

        for c in sys.Conditions do
            c.InAddress <-  addr()


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
    member __.``Allocate existing global Memory`` () =
        let t = CpuTestSample()

        let result = exportXMLforXGI(t.Sys, myTemplate "Allocate existing global Memory", None)
        result === result


    [<Test>]
    member __.``Allocate existing global IO`` () =
        let t = CpuTestSample()

        let result = exportXMLforXGI(t.Sys, myTemplate "Allocate existing global IO", Some myExistIO)
        result === result
