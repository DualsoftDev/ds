open System.IO
open System.Data
open System.IO.MemoryMappedFiles
open DsMemoryService
open System
open System.Threading
open System.ServiceProcess
open DsMemoryService.ServiceImpl
open IOMapApi


[<EntryPoint>]
let main argv = 



    let servicesToRun = [| new IOMapService() :> ServiceBase |]
    ServiceBase.Run(servicesToRun) 

    //let svc = new IOMapService()
    //svc.Load @"UnitTest\A" |> ignore
    
 
    //Console.WriteLine("MemoryIOManager.Loaded")
    //Console.ReadKey() |> ignore

    0  

