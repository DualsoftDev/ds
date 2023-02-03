// Learn more about F# at http://fsharp.org

open System
open System.IO
open Model.Import.Office
open Engine.CodeGenCPU
open Engine.Common.FS
open Engine.Core
open System.IO
open System.Linq
open Engine.Core
open Engine.Common.FS
open Engine.CodeGenCPU
open PLC.CodeGen.LSXGI
open System
open Model.Import.Office
open Engine.Parser.FS


[<EntryPoint>]
let main argv =

    let parseText (systemRepo:ShareableSystemRepository) referenceDir text =
        let helper = ModelParser.ParseFromString2(text, ParserOptions.Create4Simulation(systemRepo, referenceDir, "ActiveCpuName", None, DuNone))
        helper.TheSystem

    let testDir = @$"{__SOURCE_DIRECTORY__}\..\UnitTest.Model\ImportOfficeExample\sample"

    let loadSampleSystem(textDs:string)  =
        let systemRepo   = ShareableSystemRepository ()
        let referenceDir = testDir
        let sys = parseText systemRepo referenceDir textDs
        Runtime.System <- sys
        applyTagManager (sys, Storages())
        sys

    let sampleDirectory = testDir
    let dsPath = sampleDirectory + "\s_car.ds"
    let txt= File.ReadAllText(dsPath);
    let sys = loadSampleSystem(txt)
    let result = exportXMLforXGI(sys, "XXXXXXXXX", None)

    0 // return an integer exit code
