namespace Client

open IO.Core
open System
open System.Threading
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
      

        let testFile = Path.Combine(AppContext.BaseDirectory   , @$"../../src/UnitTest/UnitTest.Model/ImportOfficeExample/exportDS.dsz");
        let jsonPath = unZip testFile
        RuntimeDS.Package <- RuntimePackage.PCSIM;
        let target = WINDOWS, LS_XGK_IO
        let model: Model = ParserLoader.LoadFromConfig(jsonPath) WINDOWS
        let dsCPU, _, _ = DsCpuExt.GetDsCPU (model.System) target

        let _serverThread = server.Run()
        let client =  new Client($"{zmqHWInfo.ServerIP}:{zmqHWInfo.ServerPort}")
        let iospec = client.GetMeta()
        iospec|>regulate
        zmqStartDs(dsCPU, server, client)
       
        Console.ReadKey() |> ignore
        0  


