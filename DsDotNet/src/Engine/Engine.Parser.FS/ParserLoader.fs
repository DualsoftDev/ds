namespace Engine.Parser.FS

open System.IO
open Newtonsoft.Json
open Dual.Common.Core.FS
open Engine.Core
open System.Linq
open System.Collections.Generic
open System.Runtime.CompilerServices
open PathManager


[<AutoOpen>]
[<RequireQualifiedAccess>]
module ParserLoader =
    
    let private loadSystemFromDsFile (systemRepo:ShareableSystemRepository) (dsFilePath) =
        let text = File.ReadAllText(dsFilePath)
        let dir = Path.GetDirectoryName(dsFilePath)
        let option = ParserOptions.Create4Runtime(systemRepo, dir, "ActiveCpuName", Some dsFilePath, DuNone)
        let system = ModelParser.ParseFromString(text, option)
        system

    let loadingDS(loadingConfigDir:string) (activePaths: string seq) =
        let systemRepo = ShareableSystemRepository()
        let systems =
            let paths = 
                [
                    for dsFile in activePaths do 
                            if PathManager.isPathRooted (dsFile.ToFile())
                            then 
                                yield dsFile |> FileManager.fileExistChecker
                            else
                                yield PathManager.getFullPath (dsFile.ToFile()) (loadingConfigDir|>DsDirectory) |> FileManager.fileExistChecker
                ]
            paths |> List.map (loadSystemFromDsFile systemRepo)


        let loadings = systems.Collect(fun f-> f.GetRecursiveLoadeds())
                              .Map(fun s-> s.AbsoluteFilePath)
                              .Distinct().ToFSharpList()
        systems , loadings


    let LoadFromConfig(configPath: string) =
        let cfg = LoadConfig configPath
        let dir = PathManager.getDirectoryName (configPath.ToFile())
        let systems, loadings = loadingDS dir cfg.DsFilePaths
        { Config = cfg; Systems = systems; LoadingPaths = loadings}


    let LoadFromActivePath(activePath: string) =
        let dir = PathManager.getDirectoryName (activePath.ToFile())
        loadingDS dir [activePath]
