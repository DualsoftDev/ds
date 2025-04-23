namespace Engine.Runtime

open System
open Newtonsoft.Json
open System.IO
type FilePath = string


[<AutoOpen>]
module DSPServerConfigUtils =

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
    // Default configuration
    let defaultConfig = {
        OPCServerConfig = { OPCServerStartPort = "50000" }
        StatisticsConfig = { StatisticsFilePath = "%APPDATA%\\dualsoft\\DSPilot\\StatisticsData\\" }
    }
    
    // Helper function to resolve paths with environment variables
    let resolvePath (path: string) =
        Environment.ExpandEnvironmentVariables(path)

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

    let DeleteStatisticsFile(systemName:string) =
        let filePath = Path.Combine(fromServerConfig "StatisticsFilePath", $"{systemName}.json")
        if File.Exists filePath
        then 
            File.Delete filePath

        filePath