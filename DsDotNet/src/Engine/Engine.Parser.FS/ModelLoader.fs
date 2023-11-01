namespace rec Engine.Core

open System.IO
open Newtonsoft.Json
open Dual.Common.Core.FS
open Engine.Parser.FS
open System.Collections.Generic
open System.Runtime.CompilerServices
open PathManager



[<AutoOpen>]
[<RequireQualifiedAccess>]
module ModelLoader =
    let private jsonSettings = JsonSerializerSettings()

    let LoadConfig (path: FilePath) =
        let json = File.ReadAllText(path)
        JsonConvert.DeserializeObject<ModelConfig>(json, jsonSettings)

    let SaveConfig (path: FilePath) (modelConfig:ModelConfig) =
        let json = JsonConvert.SerializeObject(modelConfig, jsonSettings)
        File.WriteAllText(path, json)

    let SaveConfigWithPath (path: FilePath) (sysRunPaths: string seq) =
        let cfg =
            {
                DsFilePaths = 
                    sysRunPaths
                    |> Seq.map(fun path -> path.Replace("\\", "/"))
                    |> Seq.toList
            }
        SaveConfig path cfg 

    let private loadSystemFromDsFile (systemRepo:ShareableSystemRepository) (dsFilePath) =
        let text = File.ReadAllText(dsFilePath)
        let dir = Path.GetDirectoryName(dsFilePath)
        let option = ParserOptions.Create4Runtime(systemRepo, dir, "ActiveCpuName", Some dsFilePath, DuNone)
        let system = ModelParser.ParseFromString(text, option)
        system

    let LoadFromConfig(config: FilePath) =
        let systemRepo = ShareableSystemRepository()
        //let envPaths = collectEnvironmentVariablePaths()
        let cfg = LoadConfig config
        let dir = PathManager.getDirectoryName (config.ToFile())
        let systems =
            let paths = 
                [
                    for dsFile in cfg.DsFilePaths do 
                            if PathManager.isPathRooted (dsFile.ToFile())
                            then 
                                yield dsFile |> FileManager.fileExistChecker
                            else
                                yield PathManager.getFullPath (dsFile.ToFile()) (dir|>DsDirectory) |> FileManager.fileExistChecker

                //  for path in envPaths do
                // PathManager.getFullPath dsFile path   
                ]
            paths |> List.map (loadSystemFromDsFile systemRepo)

        { Config = cfg; Systems = systems}

    let exportLoadedSystem (s: LoadedSystem) =
       
        let absPath =  PathManager.changeExtension (s.AbsoluteFilePath.ToFile())  "ds"
        ensureDirectoryExists absPath

        let refName = s.ReferenceSystem.Name
        let libName = PathManager.getFileNameWithoutExtension(absPath.ToFile())

        s.ReferenceSystem.Name <- libName
        FileManager.fileWriteAllText(absPath, s.ReferenceSystem.ToDsText(false))
        s.ReferenceSystem.Name <- refName

        absPath

        

module private TestLoadConfig =
    let testme() =
        let cfg =
            {   DsFilePaths = [
                    @"D:\Git\ds\DsDotNet\src\UnitTest\UnitTest.Engine\Libraries\cylinder.ds"
                    @"D:\Git\ds\DsDotNet\src\UnitTest\UnitTest.Engine\Libraries\station.ds" ] }

        let fp = @"D:\tmp\a.tmp"
        ensureDirectoryExists fp
        ModelLoader.SaveConfig fp cfg

        let cfg2 = ModelLoader.LoadConfig fp

        verify (cfg = cfg2)


[<Extension>]
type ModelLoaderExt =


    [<Extension>] 
    static member pptxToExportDS (sys:DsSystem, pptPath:string) = 
       // TestLoadConfig.testme()


        let dsFile = PathManager.changeExtension (pptPath.ToFile()) ".ds" 
        let jsFile = PathManager.changeExtension (pptPath.ToFile()) ".json"
        let myDsFile = $"./{(PathManager.getFileNameWithoutExtension (pptPath.ToFile()))}.ds"   // 상대경로로 기본 저장

        for s in sys.GetRecursiveLoadeds() do
            match s with
            | :? Device -> exportLoadedSystem s |> ignore
            | :? ExternalSystem -> exportLoadedSystem s |> ignore
            | _ -> ()

        FileManager.fileWriteAllText(dsFile, sys.ToDsText(false))
        ModelLoader.SaveConfigWithPath jsFile [ myDsFile ]

        dsFile
