namespace OPC.DSServer

open Newtonsoft.Json
open System
open System.IO
open System.Collections.Generic
open Engine.Core
open Engine.Runtime

[<AutoOpen>]
module ServerConfigModule =

 

    type StatsDto = {
        Count: uint32
        Mean: float32
        MeanTemp: float32
        ActiveTime: uint32
        MovingTime: uint32
    }




    let GetOPCServerPort(mode: RuntimeMode, targetIp:string) = 
        let serverStartPort = fromServerConfig "OPCServerStartPort" |> Convert.ToInt32
        let serverPort =
            let lastIp =
                match targetIp.Split('.') |> Array.tryLast with
                | Some ipStr when System.Int32.TryParse(ipStr) |> fst -> int ipStr
                | _ -> 0

            match mode with
            | RuntimeMode.Control        -> serverStartPort + 1000  + lastIp
            | RuntimeMode.VirtualPlant   -> serverStartPort + 2000  + lastIp
            | RuntimeMode.Monitoring     -> serverStartPort + 3000  + lastIp
            | RuntimeMode.Simulation   
            | RuntimeMode.VirtualLogic   -> 2747 //  Monitoring, Simulation, Virtual Logic Port 고정

        serverPort

    let GetOPCServerPortByModeText(mode: string, targetIp:string) = 
        GetOPCServerPort (ToRuntimeMode mode, targetIp)

    // Save statistics to a JSON file
    let SaveStatisticsToJson ((systemName: string), (statsMap: IDictionary<string, StatsDto>)) =
        let filePath = Path.Combine(fromServerConfig "StatisticsFilePath", $"{systemName}.json")
        let directory = Path.GetDirectoryName(filePath)
    
        // Ensure the directory exists
        if not (Directory.Exists(directory)) then
            Directory.CreateDirectory(directory) |> ignore

        let statsDictionary =
            statsMap
            |> Seq.map (fun kvp -> kvp.Key, kvp.Value)
            |> dict

        let json = JsonConvert.SerializeObject(statsDictionary, Formatting.Indented)
        File.WriteAllText(filePath, json)

    // Load statistics from a JSON file
    let LoadStatisticsFromJson (systemName: string) : Dictionary<string, StatsDto> =
        let filePath = Path.Combine(fromServerConfig "StatisticsFilePath", $"{systemName}.json")
        if File.Exists(filePath) then
            let json = File.ReadAllText(filePath)
            JsonConvert.DeserializeObject<Dictionary<string, StatsDto>>(json)
        else
            Dictionary<string, StatsDto>()
