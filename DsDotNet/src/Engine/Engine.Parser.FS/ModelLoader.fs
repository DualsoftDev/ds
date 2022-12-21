namespace rec Engine.Core

open System.IO
open Newtonsoft.Json
open Engine.Common.FS
open Engine.Parser.FS

[<AutoOpen>]
module ModelLoaderModule =
    type FilePath = string
    type ModelConfig = {
        DsFilePaths: FilePath list
    }
    type Model = {
        Config: ModelConfig
        Systems: DsSystem list
    }


[<RequireQualifiedAccess>]
module ModelLoader =
    let private jsonSettings = JsonSerializerSettings()

    let LoadConfig (path: FilePath) =
        let json = File.ReadAllText(path)
        JsonConvert.DeserializeObject<ModelConfig>(json, jsonSettings)

    let SaveConfig (path: FilePath) (modelConfig:ModelConfig) =
        let json = JsonConvert.SerializeObject(modelConfig, jsonSettings)
        File.WriteAllText(path, json)


    let private loadSystemFromDsFile (systemRepo:ShareableSystemRepository) (dsFilePath) =
        let text = File.ReadAllText(dsFilePath)
        let dir = Path.GetDirectoryName(dsFilePath)
        let option = ParserOptions.Create4Runtime(systemRepo, dir, "ActiveCpuName", Some dsFilePath)
        let system = ModelParser.ParseFromString(text, option)
        system

    let LoadFromConfig(config: FilePath) =
        let cfg = LoadConfig config
        let systemRepo = ShareableSystemRepository()

        let systems =
            [   for dsFile in cfg.DsFilePaths do
                    loadSystemFromDsFile systemRepo dsFile ]
        { Config = cfg; Systems = systems }

module private TestLoadConfig =
    let testme() =
        let cfg =
            {   DsFilePaths = [
                    @"F:\Git\ds\DsDotNet\src\UnitTest\UnitTest.Engine\Libraries\cylinder.ds"
                    @"F:\Git\ds\DsDotNet\src\UnitTest\UnitTest.Engine\Libraries\station.ds" ] }

        let fp = @"F:\tmp\a.tmp"
        ModelLoader.SaveConfig fp cfg

        let cfg2 = ModelLoader.LoadConfig fp

        verify (cfg = cfg2)