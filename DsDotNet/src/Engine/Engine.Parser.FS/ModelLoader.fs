namespace rec Engine.Core

open System.IO
open Newtonsoft.Json
open Dual.Common.Core.FS
open Engine.Parser.FS
open System.Collections.Generic

[<AutoOpen>]
module ModelLoaderModule =
    type FilePath = string
    type ModelConfig = {
        DsFilePaths: FilePath list
    }
    type Model = {
        Config: ModelConfig
        Systems : DsSystem list
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
        let option = ParserOptions.Create4Runtime(systemRepo, dir, "ActiveCpuName", Some dsFilePath, DuNone)
        let system = ModelParser.ParseFromString(text, option)
        system

    let LoadFromConfig(config: FilePath) =
        let systemRepo = ShareableSystemRepository()
        let envPaths = collectEnvironmentVariablePaths()
        let cfg = LoadConfig config
        let dirs = config.Replace('/', '\\').Split('\\') |> List.ofArray
        let dir = StringExt.JoinWith(dirs.RemoveAt(dirs.Length - 1), "\\")
        let systems =
            [
                for dsFile in cfg.DsFilePaths do 
                    [
                        dsFile;
                        $"{dir}\\{dsFile}";
                        for path in envPaths do
                            $"{path}\\{dsFile}"
                    ] 
                    |> fileExistChecker
                    |> loadSystemFromDsFile systemRepo
            ]
        { Config = cfg; Systems = systems}

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