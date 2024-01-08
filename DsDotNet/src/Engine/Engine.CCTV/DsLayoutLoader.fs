module DsLayoutLoaderModule

open Engine.Core
open Engine.Info
open Engine.Parser.FS
open System.IO
open System.Collections.Generic
open System.Linq
open System
open Engine.CodeGenCPU
open Emgu.CV
open OpenCVUtils

[<Flags>]    
type ViewType =
    | Normal = 0
    | Error  = 1


type ServerImage(channelName, screenType, url) =
    member x.ChannelName :string  = channelName
    member x.ScreenType :ScreenType  = screenType
    member x.URL :string = url
    //이미지는 DsLayoutLoader여기서 받고 CCTV는 streamingBackFrame 여기서 할당받음
    member val ImageScreen: Mat= new Mat() with get, set 

type DsLayoutLoader() =
    let mutable _dsSystem:DsSystem option = None
    let serverImages = HashSet<ServerImage>()

    let _lockBackFrame = obj()
    let getBackFrameOrNotNullUpdate(channelName:string, frame:Mat option)  =
        lock _lockBackFrame (fun () ->
            let serverImage = serverImages.First(fun f->f.ChannelName =channelName)
            if frame.IsSome then
                serverImage.ImageScreen  <- frame.Value
        
            serverImage.ImageScreen 
        )

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
        chs.ForEach(fun info ->
                let screenImage = ServerImage(info.ChannelName, info.ScreenType, info.Path)
                screenImage.ImageScreen <-
                    if info.ScreenType = ScreenType.IMAGE 
                    then 
                        let imgPath = PathManager.combineFullPathFile([|dsFileDir;$"{info.ChannelName}.jpg"|])
                        File.ReadAllBytes(imgPath) |> OpenCVUtils.ByteArrayToMat 
                    else new Mat()

                serverImages.Add(screenImage) |>ignore          
        )
                

    member x.DsSystem = _dsSystem.Value
    member x.LayoutInfos = _dsSystem.Value.LayoutInfos
    member x.GetBackFrame(ch:string, frame:Mat option) = getBackFrameOrNotNullUpdate(ch, frame) 

    member x.GetViewTypeList() =  Enum.GetNames(typeof<ViewType>) 

    member x.GetServerChannels()       = serverImages.Select(fun f->f.ChannelName)
    member x.GetUrl(ch:string)         = serverImages.First(fun f-> f.ChannelName = ch).URL
    member x.GetChannelName(ch:string) = serverImages.First(fun f-> f.ChannelName = ch).ChannelName
    member x.GetScreenType(ch:string)  = serverImages.First(fun f-> f.ChannelName = ch).ScreenType
    member x.GetImage(ch:string)       = serverImages.First(fun f-> f.ChannelName = ch).ImageScreen

    member x.ExistImageScreenData(ch:string) = x.GetImage(ch).IsEmpty |> not
    