namespace OPC.DSServer

open Newtonsoft.Json
open System
open System.IO
open System.Collections.Generic

[<AutoOpen>]
module ServerConfigModule =

    type OPCServerConfig = {
        OPCServerPort: string
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
    }

    // Helper function to resolve paths with environment variables
    let resolvePath (path: string) =
        Environment.ExpandEnvironmentVariables(path)

    // Default configuration
    let defaultConfig = {
        OPCServerConfig = { OPCServerPort = "2747" }
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
        | "OPCServerPort" -> config.OPCServerConfig.OPCServerPort
        | "StatisticsFilePath" -> resolvePath config.StatisticsConfig.StatisticsFilePath
        | _ -> failwithf "Key '%s' not found in configuration" key

    let fromServerConfig (key: string) : string =
        let configPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "dualsoft/DSPilot/ServerConfig.json");
        let config = loadConfig configPath
        getValueFromConfig key config


    let GetServerPort() = 
        fromServerConfig "OPCServerPort"

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
