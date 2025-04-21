namespace OPC.DSServer

open Newtonsoft.Json
open System
open System.IO
open System.Collections.Generic
open Engine.Core

[<AutoOpen>]
module ServerConfigModule =

    type OPCServerConfig = {
        OPCServerStartPort: string
    }

    type StatisticsConfig = {
        StatisticsFilePath: string
    }

    type Configuration = {
        OPCServerConfig: OPCServerConfig
        StatisticsConfig: StatisticsConfig
    }

    type StatsDto = {
        Count: uint32
        Mean: float32
        MeanTemp: float32
        ActiveTime: uint32
        MovingTime: uint32
    }

    // Helper function to resolve paths with environment variables
    let resolvePath (path: string) =
        Environment.ExpandEnvironmentVariables(path)

    // Default configuration
    let defaultConfig = {
        OPCServerConfig = { OPCServerStartPort = "50000" }
        StatisticsConfig = { StatisticsFilePath = "%APPDATA%\\dualsoft\\DSPilot\\StatisticsData\\" }
    }

    // Save configuration to a JSON file
    let saveConfig (filePath: string) (config: Configuration) =
        let json = JsonConvert.SerializeObject(config, Formatting.Indented)
        File.WriteAllText(filePath, json)

    // Load configuration from a JSON file, or create a default configuration if not present
    let loadConfig (filePath: string) : Configuration =
        if File.Exists(filePath) then
            let json = File.ReadAllText(filePath)
            JsonConvert.DeserializeObject<Configuration>(json)
        else
            saveConfig filePath defaultConfig
            defaultConfig

    // Retrieve a specific configuration value by key
    let getValueFromConfig (key: string) (config: Configuration) : string =
        match key with
        | "OPCServerStartPort" -> config.OPCServerConfig.OPCServerStartPort
        | "StatisticsFilePath" -> resolvePath config.StatisticsConfig.StatisticsFilePath
        | _ -> failwithf "Key '%s' not found in configuration" key


    let fromServerConfig (key: string) : string =
        let appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        let configDirectory = Path.Combine(appDataPath, "dualsoft/DSPilot")
        let configFilePath = Path.Combine(configDirectory, "ServerConfig.json")
            
        // Ensure the directory exists
        if not (Directory.Exists(configDirectory)) then
            Directory.CreateDirectory(configDirectory) |> ignore

        // Load configuration and get value
        let config = loadConfig configFilePath
        getValueFromConfig key config



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
