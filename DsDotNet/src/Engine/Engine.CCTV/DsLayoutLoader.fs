module DsLayoutLoaderModule

open Engine.Core
open Engine.Info
open Engine.Parser.FS
open System.IO
open System.Collections.Generic
open System.Linq
open System
open Engine.CodeGenCPU

[<Flags>]    
type ViewType =
    | Normal = 0
    | Error  = 1

[<AutoOpen>]
type ScreenInfo = {
        Id :int 
        IpPort : string
        URL :string 
        ViewType : ViewType
    }
    with member x.Key = $"{x.Id}:{x.IpPort}";


[<AutoOpen>]
type CCTVInfo() =
    member val Id = 0 with get, set
    member val URL = "" with get, set


    
type DsLayoutLoader() =
    let mutable _dsSystem:DsSystem option = None

    do
        let commonAppSettings = DSCommonAppSettings.Load(Path.Combine(AppContext.BaseDirectory, "CommonAppSettings.json"))
        let connectionString = commonAppSettings.LoggerDBSettings.ConnectionString
        let dsFileJson = DBLogger.GetDsFilePath(connectionString)

        let model = ParserLoader.LoadFromConfig(dsFileJson)
        _dsSystem <- Some model.System
        CpuLoaderExt.LoadStatements(model.System, Storages()) |> ignore
        let querySet = QuerySet(CommonAppSettings = commonAppSettings)
        DBLogger.InitializeLogReaderOnDemandAsync(querySet, [model.System] |> List).Result |> ignore

    member x.DsSystem = _dsSystem.Value
    member x.LayoutInfos = _dsSystem.Value.LayoutInfos

    member x.GetScreens() =
        let screens = HashSet<CCTVInfo>()
        let chs = x.DsSystem.LayoutChannels.ToList()
        for i = 1 to chs.Count  do
            screens.Add(new CCTVInfo(Id = i, URL = $"{chs.[i-1]}")) |> ignore

        screens

    member x.GetScreenUrl(id:int) =
        x.GetScreens().First(fun f-> f.Id = id).URL

    member x.GetViewTypeList() = 
        Enum.GetNames(typeof<ViewType>) 