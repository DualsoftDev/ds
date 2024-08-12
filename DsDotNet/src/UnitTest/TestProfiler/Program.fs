// Learn more about F# at http://fsharp.org

open System
open System.IO
open Engine.Import.Office
open Engine.CodeGenCPU
open Engine.CodeGenPLC
open Dual.Common.Core.FS
open Engine.Core
open System.IO
open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open Engine.CodeGenCPU
open PLC.CodeGen.LS
open System
open Engine.Import.Office
open Engine.Parser.FS


[<EntryPoint>]
let main argv =

    let parseText (systemRepo:ShareableSystemRepository) referenceDir text =
        ModelParser.ParseFromString(text, ParserOptions.Create4Simulation(systemRepo, referenceDir, "ActiveCpuName", None, DuNone))

    let testDir = @$"{__SOURCE_DIRECTORY__}/../UnitTest.Model/ImportOfficeExample/sample"

    let loadSampleSystem(textDs:string)  =
        let systemRepo   = ShareableSystemRepository ()
        let referenceDir = testDir
        let sys = parseText systemRepo referenceDir textDs
        RuntimeDS.System <- sys
        applyTagManager (sys, Storages(), (WINDOWS, LS_XGK_IO))
        sys

    let sampleDirectory = testDir
    let dsPath = sampleDirectory + "/s_car.ds"
    let txt= File.ReadAllText(dsPath);
    let sys = loadSampleSystem(txt)
    let result = exportXMLforLSPLC(XGI, sys, "XXXXXXXXX", None,  0, 0)

    0 // return an integer exit code

    //let testAddressSetting (sys:DsSystem) =
    //    for j in sys.Jobs do
    //        for dev in j.TaskDefs do
    //        if dev.ApiItem.RXs.any() then  dev.InAddress <- "%MX777"
    //        if dev.ApiItem.TXs.any() then  dev.OutAddress <- "%MX888"

    //    for b in sys.Buttons do
    //        b.InAddress <- "%MX777"
    //        b.OutAddress <- "%MX888"

    //    for l in sys.Lamps do
    //        l.OutAddress <- "%MX888"

    //    for c in sys.Conditions do
    //        c.InAddress <- "%MX777"

    //let testDir = @$"{__SOURCE_DIRECTORY__}/../UnitTest.Model/ImportOfficeExample/sample"

    //let sampleDirectory = testDir
    //let myTemplate testName = Path.Combine($"{__SOURCE_DIRECTORY__}", $"../UnitTest.PLC.Xgx/XgiXmls/{testName}.xml")
    //let pptPath = sampleDirectory + "/s_car.pptx"
    //let model = ImportPpt.GetModel [ pptPath ]
    //model.Systems.ForEach(testAddressSetting)

    ////let result = exportXMLforXGI(model.Systems.First(), myTemplate "XXXXXXXXX", None)

    //0 // return an integer exit code

