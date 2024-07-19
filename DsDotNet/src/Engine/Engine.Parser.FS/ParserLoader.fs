namespace Engine.Parser.FS

open System.IO
open Dual.Common.Core.FS
open Engine.Core
open System.Linq
open System.Diagnostics


[<AutoOpen>]
[<RequireQualifiedAccess>]
module ParserLoader =

    let private loadSystemFromDsFile (systemRepo: ShareableSystemRepository) (dsFilePath) autoGenDevice =
        let text = File.ReadAllText(dsFilePath)

        if text.TrimStart().StartsWith("[sys]")
        then 
            let dir = Path.GetDirectoryName(dsFilePath)
            let option =
                ParserOptions.Create4Runtime(systemRepo, dir, "ActiveCpuName", Some dsFilePath, DuNone, autoGenDevice, false)

            let system = ModelParser.ParseFromString(text, option)
            system
        else
            failwithf $"Invalid ds file format \r\n ds format is [sys] ... \r\n {dsFilePath}"

    let loadingDS (loadingConfigDir: string) (dsFile: string )  autoGenDevice (target:PlatformTarget)=
        ParserUtil.runtimeTarget  <- target
        let systemRepo = ShareableSystemRepository()

        let sysPath =
            if PathManager.isPathRooted (dsFile) then
                dsFile |> FileManager.fileExistChecker
            else
                PathManager.getFullPath (dsFile.ToFile()) (loadingConfigDir |> DsDirectory)
                |> FileManager.fileExistChecker 

        let system = loadSystemFromDsFile  systemRepo sysPath autoGenDevice

        let loadings =
                system.GetRecursiveLoadeds().Map(fun s -> s.AbsoluteFilePath)
                                            .Distinct()
                                            .ToFSharpList()

        system, loadings


    let LoadFromConfig (configPath: string) (target:PlatformTarget)=
        let jsonFileName =PathManager.getFileName (configPath.ToFile())
        if jsonFileName  <> TextDSJson
        then failwithf $"LoadFromConfig FileName must be {TextDSJson}: now {jsonFileName}"

        let configPath = $"{PathManager.getDirectoryName (configPath.ToFile())}{TextDSJson}"
        let cfg = LoadConfig configPath
        let dir = PathManager.getDirectoryName (configPath.ToFile())
        let system, loadings = loadingDS dir cfg.DsFilePath false target

        { Config = cfg
          System = system
          LoadingPaths = loadings }
          

    let LoadFromActivePath (activePath: string) (target:PlatformTarget) (usingGpt:bool)=
        ModelParser.ClearDicParsingText()

        let stopwatch = Stopwatch()
        stopwatch.Start()
     

        let dir = PathManager.getDirectoryName (activePath.ToFile())
        let ret = loadingDS dir activePath  usingGpt  target 

        stopwatch.Stop()
        stopwatch.ElapsedMilliseconds |> printfn "Elapsed time: %d ms"
        ret

    let LoadFromDevicePath (activePath: string) (target:PlatformTarget)=
        let dir = PathManager.getDirectoryName (activePath.ToFile())
        loadingDS dir activePath  false  target 

    let LoadFromChatGptPath (activePath: string) (target:PlatformTarget)=
        LoadFromActivePath activePath target true

        