open System.IO
open System.Data
open System.IO.MemoryMappedFiles
open DsMemoryService
open System
open System.Threading
open System.ServiceProcess
open DsMemoryService.ServiceImpl


[<EntryPoint>]
let main argv = 


    let servicesToRun = [| new IOMapService() :> ServiceBase |]
    ServiceBase.Run(servicesToRun)

    //let svc = new IOMapService()
    ////test debug 관리자 권한에서 실행
    //getAllRelativeFiles 
    //|> Array.iter (svc.Load)
 
    //Console.WriteLine("MemoryIOManager.Loaded")
    //Console.ReadKey() |> ignore

    0  

