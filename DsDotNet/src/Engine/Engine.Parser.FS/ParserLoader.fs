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

    let private loadSystemFromDsFile (systemRepo: ShareableSystemRepository) (dsFilePath) =
        let text = File.ReadAllText(dsFilePath)
        let dir = Path.GetDirectoryName(dsFilePath)

        let option =
            ParserOptions.Create4Runtime(systemRepo, dir, "ActiveCpuName", Some dsFilePath, DuNone)

        let system = ModelParser.ParseFromString(text, option)
        system

    let loadingDS (loadingConfigDir: string) (dsFile: string ) =
        let systemRepo = ShareableSystemRepository()

        let sysPath =
            if PathManager.isPathRooted (dsFile.ToFile()) then
                dsFile |> FileManager.fileExistChecker
            else
                PathManager.getFullPath (dsFile.ToFile()) (loadingConfigDir |> DsDirectory)
                |> FileManager.fileExistChecker 

        let system = loadSystemFromDsFile  systemRepo sysPath

        let loadings =
                system.GetRecursiveLoadeds().Map(fun s -> s.AbsoluteFilePath)
                                            .Distinct()
                                            .ToFSharpList()

        system, loadings


    let LoadFromConfig (configPath: string) =
        let configPath = $"{PathManager.getDirectoryName (configPath.ToFile())}{TextDSJson}"
        let cfg = LoadConfig configPath
        let dir = PathManager.getDirectoryName (configPath.ToFile())
        let system, loadings = loadingDS dir cfg.DsFilePath

        { Config = cfg
          System = system
          LoadingPaths = loadings }


    let LoadFromActivePath (activePath: string) =
        let dir = PathManager.getDirectoryName (activePath.ToFile())
        loadingDS dir   activePath 
