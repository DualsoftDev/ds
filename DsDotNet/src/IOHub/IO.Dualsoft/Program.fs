namespace Client

open IO.Core
open System
open System.Linq
open System.Threading
open ZmqTestModule
open Dual.Common.Core.FS
open IOClient.DS.ScanDSImpl
open Engine.Core
open System.IO
open Engine.Parser.FS
open Engine.Cpu

module ZmqTestClient =
  
    [<EntryPoint>]
    let main _ = 
        let zmqHWInfo = IOSpecHW.FromJsonFile "zmqhw.json"
        
        let client =  new Client($"{zmqHWInfo.ServerIP}:{zmqHWInfo.ServerPort}")

        let testFile = Path.Combine(AppContext.BaseDirectory   , @$"../../src/UnitTest/UnitTest.Model/ImportOfficeExample/exportDS.Zip");

        let jsonPath = unZip testFile
        RuntimeDS.Package <- RuntimePackage.Simulation;
        let model: Model = ParserLoader.LoadFromConfig(jsonPath)
        let dsCPU, _ = DsCpuExt.GetDsCPU(model.System)
        


        let iospec = client.GetMeta()
        iospec|>regulate
        let vendor = iospec.Vendors.First(fun f->f.Location = "")
        IoEventDS(dsCPU, vendor, client) |>ignore

        Console.ReadKey()
        0  


