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
        ChannelName :string 
        ScreenType : ScreenType
        ViewType : ViewType
        URL :string 
        IpPort : string 
    }
    with member x.Key =  $"{x.Id}:{x.IpPort}";

type ScreenImage = {
        ChannelName :string 
        ImageScreen : byte[]
    }

type DsLayoutLoader() =
    let mutable _dsSystem:DsSystem option = None
    let screens = HashSet<ScreenInfo>()
    let screenImages = HashSet<ScreenImage>()

    do
        let commonAppSettings = DSCommonAppSettings.Load(Path.Combine(AppContext.BaseDirectory, "CommonAppSettings.json"))
        let connectionString = commonAppSettings.LoggerDBSettings.ConnectionString
        let dsFileJson = DBLogger.GetDsFilePath(connectionString)
        let dsFileDir  = PathManager.getDirectoryName(dsFileJson |>DsFile)

        let model = ParserLoader.LoadFromConfig(dsFileJson)
        _dsSystem <- Some model.System
        CpuLoaderExt.LoadStatements(model.System, Storages()) |> ignore
        let querySet = QuerySet(CommonAppSettings = commonAppSettings)
        DBLogger.InitializeLogReaderOnDemandAsync(querySet, [model.System] |> List).Result |> ignore

        let chs = _dsSystem.Value.LayoutInfos.DistinctBy(fun f->f.ChannelName+f.Path).ToList()
        for i = 1 to chs.Count  do
            let info = chs.[i-1]
            if info.ScreenType = ScreenType.IMAGE 
            then   
                let imgPath = PathManager.combineFullPathFile([|dsFileDir;$"{info.ChannelName}.jpg"|])
                let screenImage = {ChannelName = info.ChannelName; ImageScreen = File.ReadAllBytes(imgPath)}
                screenImages.Add(screenImage) |>ignore               
            let screenInfo = {Id = i; IpPort = "";  ChannelName = info.ChannelName; URL = info.Path; ScreenType = info.ScreenType; ViewType = ViewType.Normal}
            screens.Add(screenInfo) |> ignore


    member x.DsSystem = _dsSystem.Value
    member x.LayoutInfos = _dsSystem.Value.LayoutInfos
    member x.GetViewTypeList() =  Enum.GetNames(typeof<ViewType>) 

    member x.GetScreens() = screens
    member x.GetUrl(id:int)         = screens.First(fun f-> f.Id = id).URL
    member x.GetChannelName(id:int) = screens.First(fun f-> f.Id = id).ChannelName
    member x.GetScreenType(id:int)  = screens.First(fun f-> f.Id = id).ScreenType
    member x.GetImage(channelName:string) = screenImages.First(fun f-> f.ChannelName = channelName).ImageScreen

