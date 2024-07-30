namespace Engine.Parser.FS

open System.IO
open Dual.Common.Core.FS
open Engine.Core
open System.Linq
open System.Diagnostics


[<AutoOpen>]
[<RequireQualifiedAccess>]
module ParserLoader =

    let private loadSystemFromDsFile (systemRepo: ShareableSystemRepository) dsFilePath (loadedName:string option) autoGenDevice =

        let text = File.ReadAllText(dsFilePath)

        if text.TrimStart().StartsWith("[sys]") then 
            let dir = Path.GetDirectoryName(dsFilePath)
            let option =
                if loadedName.IsSome then
                    ParserOptions.Create4RuntimeLoadedSystem(systemRepo, dir, "ActiveCpuName", Some dsFilePath, DuNone, autoGenDevice, loadedName.Value)
                else 
                    ParserOptions.Create4Runtime(systemRepo, dir, "ActiveCpuName", Some dsFilePath, DuNone, autoGenDevice, false)
                    

            let system = ModelParser.ParseFromString(text, option)
            system
        else
            failwithf $"Invalid ds file format \r\n ds format is [sys] ... \r\n {dsFilePath}"

    let loadingDS (loadingConfigDir: string)  (dsFile: string ) (loadedName: string option)  autoGenDevice (target:PlatformTarget)=
        ParserUtil.runtimeTarget  <- target
        let systemRepo = ShareableSystemRepository()

        let sysPath =
            if PathManager.isPathRooted (dsFile) then
                dsFile |> FileManager.fileExistChecker
            else
                PathManager.getFullPath (dsFile.ToFile()) (loadingConfigDir |> DsDirectory)
                |> FileManager.fileExistChecker 

        let system = loadSystemFromDsFile  systemRepo sysPath loadedName autoGenDevice

        let loadings =
            system.GetRecursiveLoadeds()
                .Map(fun s -> s.AbsoluteFilePath)
                .Distinct()
                .ToFSharpList()
        system.Loading <- false
        system, loadings


    let LoadFromConfig (configPath: string) (target:PlatformTarget)=
        let jsonFileName =PathManager.getFileName (configPath.ToFile())
        if jsonFileName  <> TextDSJson then
            failwithf $"LoadFromConfig FileName must be {TextDSJson}: now {jsonFileName}"

        let configPath = $"{PathManager.getDirectoryName (configPath.ToFile())}{TextDSJson}"
        let cfg = LoadConfig configPath
        let dir = PathManager.getDirectoryName (configPath.ToFile())
        let system, loadings = loadingDS dir  cfg.DsFilePath None false target

        {
            Config = cfg
            System = system
            LoadingPaths = loadings
        }
          

    let LoadFromActivePath (activePath: string) (target:PlatformTarget) (usingGpt:bool)=
        ModelParser.ClearDicParsingText()

        let f() = 
            let dir = PathManager.getDirectoryName (activePath.ToFile())
            loadingDS dir activePath None usingGpt target 

        let ret, millisecond = duration f
        printfn $"Elapsed time: {millisecond} ms"
        ret


    let LoadFromDevicePath (activePath: string) (loadedName: string) (target:PlatformTarget)=
        let dir = PathManager.getDirectoryName (activePath.ToFile())
        loadingDS dir activePath (Some(loadedName)) false  target 

    let LoadFromChatGptPath (activePath: string) (target:PlatformTarget)=
        LoadFromActivePath activePath  target true

        