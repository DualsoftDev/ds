namespace DsMemoryService

open System
open System.IO
open System.ServiceProcess
open System.Diagnostics

module HwServiceManagerImpl =

    let IOMapServiceRun() =
        let serviceName = "IOMapService"
        let services = ServiceController.GetServices()
        let service = services |> Array.tryFind (fun s -> s.ServiceName = serviceName)
        let batFilePath = Path.Combine(__SOURCE_DIRECTORY__, "IOMapServiceNet48.bat")
        let timeout = TimeSpan.FromSeconds(5.0) // 5초 동안 대기 가능

        let waitForServiceToRun() =
            let proc = new Process()
            let startInfo = new ProcessStartInfo("powershell", $"Start-Process -FilePath '{batFilePath}' -Verb runas -Wait")
            startInfo.WindowStyle <- ProcessWindowStyle.Hidden
            startInfo.CreateNoWindow <- true
            proc.StartInfo <- startInfo
            proc.Start() |> ignore
            proc.WaitForExit()

        match service with
        | Some(service) when service.Status <> ServiceControllerStatus.Running ->
            waitForServiceToRun()

        | Some(_) ->
            Console.WriteLine($"IOMapService is already running.")

        | None ->
            Console.WriteLine($"{serviceName} not found. Attempting to start using BAT file.")
            waitForServiceToRun()
         