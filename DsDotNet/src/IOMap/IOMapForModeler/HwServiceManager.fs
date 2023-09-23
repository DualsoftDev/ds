namespace IOMapForModeler

open System
open System.IO
open System.ServiceProcess
open System.Diagnostics

module HwServiceManagerImpl =

    let private waitForServiceWithBat(batFilePath: string) =
        let proc = new Process()
        let startInfo = new ProcessStartInfo("powershell", $"Start-Process -FilePath '{batFilePath}' -Verb runas -Wait")
        startInfo.WindowStyle <- ProcessWindowStyle.Hidden
        startInfo.CreateNoWindow <- true
        proc.StartInfo <- startInfo
        proc.Start() |> ignore
        proc.WaitForExit()

    let private getServiceStatus(serviceName: string) =
        let services = ServiceController.GetServices()
        services |> Array.tryFind (fun s -> s.ServiceName = serviceName)
    
    let serviceName = "IOMapService"

    let IOMapServiceRun() =
        let batFilePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "IOMapService.bat")
    
        if File.Exists(batFilePath) then
            match getServiceStatus(serviceName) with
            | Some(service) when service.Status <> ServiceControllerStatus.Running -> waitForServiceWithBat(batFilePath)
            | Some(_) -> Console.WriteLine($"IOMapService is already running.")
            | None -> 
                Console.WriteLine($"{serviceName} not found. Attempting to start using BAT file.")
                waitForServiceWithBat(batFilePath)
        else
            failwithf ($"BAT file not found at {batFilePath}.")


    let IOMapServiceDelete() =
        let batFilePath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "IOMapService_delete.bat")
    
        if File.Exists(batFilePath) then
            match getServiceStatus(serviceName) with
            | Some(_) -> waitForServiceWithBat(batFilePath)
            | None -> Console.WriteLine($"{serviceName} not found. Attempting to delete using BAT file.")
        else
            failwithf ($"BAT file not found at {batFilePath}.")
