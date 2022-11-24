namespace rec Engine.Core

open System.IO
open Newtonsoft.Json
open Engine.Common.FS
open Engine.Parser.FS

[<AutoOpen>]
module ModelLoaderModule =
    // #r @"..\..\..\exLib\Newtonsoft.Json.dll"
    let jsonSettings = JsonSerializerSettings()


    type FilePath = string
    type ModelConfig = {
        DsFilePaths: FilePath list
    }

    let loadConfig (path: FilePath) =
        let json = File.ReadAllText(path)
        JsonConvert.DeserializeObject<ModelConfig>(json, jsonSettings)

    let saveConfig (path: FilePath) (modelConfig:ModelConfig) =
        let json = JsonConvert.SerializeObject(modelConfig, jsonSettings)
        File.WriteAllText(path, json)

    type Model = {
        Config: ModelConfig
        Systems: DsSystem list
    }

    let loadSystemFromDsFile (dsFilePath) =
        let text = File.ReadAllText(dsFilePath)
        let dir = Path.GetDirectoryName(dsFilePath)
        let option = ParserOptions.Create4Runtime(dir, "ActiveCpuName")
        let system = ModelParser.ParseFromString(text, option)
        system

    let loadModelFromConfig(config: FilePath) =
        let cfg = loadConfig config
        let systems =
            [   for dsFile in cfg.DsFilePaths do
                    loadSystemFromDsFile dsFile ]
        { Config = cfg; Systems = systems }

module private TestLoadConfig =
    let testme() =
        let cfg =
            {   DsFilePaths = [
                    @"F:\Git\ds\DsDotNet\src\UnitTest\UnitTest.Engine\Libraries\cylinder.ds"
                    @"F:\Git\ds\DsDotNet\src\UnitTest\UnitTest.Engine\Libraries\station.ds" ] }

        let fp = @"F:\tmp\a.tmp"
        saveConfig fp cfg

        let cfg2 = loadConfig fp

        verify (cfg = cfg2)