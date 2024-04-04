namespace Client

open IO.Core
open System
open System.Linq
open System.Threading
open ZmqTestModule
open Dual.Common.Core.FS

module ZmqTestClient =
  
    [<EntryPoint>]
    let main _ = 
        let zmqHWInfo = IOSpecHW.FromJsonFile "zmqhw.json"
        
        let client =  new Client($"{zmqHWInfo.ServerIP}:{zmqHWInfo.ServerPort}")

        //registerCancelKey cts client
        //clientKeyboardLoop client cts.Token

        let iospec = client.GetMeta()
        iospec|>regulate
        let vendor = iospec.Vendors.First(fun f->f.Location =  "xgi")
        let hwConnStr = $"{zmqHWInfo.HwIP}:{zmqHWInfo.HwPort}"
        let scanIO = IOClient.Xgi.ScanImpl.ScanIO(hwConnStr, vendor, client)
        let eventIO = IOClient.Xgi.IoEventXGIImpl.IoEventXGI(hwConnStr, iospec.Vendors, client)

        scanIO.DoScan()
        Console.ReadKey() |> ignore
        0  


