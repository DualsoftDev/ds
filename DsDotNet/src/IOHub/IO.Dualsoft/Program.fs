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
open Newtonsoft.Json
open ZmqStartDs


module ZmqTestClient =
  
    [<EntryPoint>]
    let main _ = 
        let stoppingToken = CancellationToken()
        let zmqPath = Path.Combine(AppContext.BaseDirectory, "zmqsettings.json")
        let specTxt = File.ReadAllTextAsync(zmqPath, stoppingToken).Result
        let ioSpec = JsonConvert.DeserializeObject<IOSpec>(specTxt)
        let server = new Server(ioSpec, stoppingToken)


        let zmqHWInfo = IOSpecHW.FromJsonFile "zmqhw.json"
      

        let testFile = Path.Combine(AppContext.BaseDirectory   , @$"../../src/UnitTest/UnitTest.Model/ImportOfficeExample/exportDS.Zip");
        let jsonPath = unZip testFile
        RuntimeDS.Package <- RuntimePackage.Simulation;
        let model: Model = ParserLoader.LoadFromConfig(jsonPath)
        let dsCPU, _ = DsCpuExt.GetDsCPU(model.System)

        let serverThread = server.Run()
        let client =  new Client($"{zmqHWInfo.ServerIP}:{zmqHWInfo.ServerPort}")
        let iospec = client.GetMeta()
        iospec|>regulate
        zmqStartDs(dsCPU, server, client)
       
        Console.ReadKey()
        0  


