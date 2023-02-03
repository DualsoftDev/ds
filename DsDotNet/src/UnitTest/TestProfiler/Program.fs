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


[<EntryPoint>]
let main argv =
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

    let sampleDirectory = Path.Combine($"{__SOURCE_DIRECTORY__}", "../UnitTest.Engine/ImportOffice/sample/");
    let myTemplate testName = Path.Combine($"{__SOURCE_DIRECTORY__}", $"../UnitTest.PLC.Xgi/XgiXmls/{testName}.xml")
    let pptPath = sampleDirectory + "s_car.pptx"
    let model = ImportPPT.GetModel [ pptPath ]
    model.Systems.ForEach(testAddressSetting)

    let result = exportXMLforXGI(model.Systems.First(), myTemplate "XXXXXXXXX", None)

    0 // return an integer exit code
