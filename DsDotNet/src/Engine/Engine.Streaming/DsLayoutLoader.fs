module DsLayoutLoaderModule

open Engine.Core
open Engine.Info
open Engine.Parser.FS
open System.IO
open System.Collections.Generic
open System.Linq
open System
open System.Drawing
open Engine.CodeGenCPU
open Emgu.CV
open OpenCVUtils

[<Flags>]    
type ViewType =
    | Normal = 0
    | Error  = 1

let _StreamSize = Size(1920, 1080)

type ServerImage(channelName, screenType, url) =
    member x.ChannelName :string  = channelName
    member x.ScreenType :ScreenType  = screenType
    member x.URL :string = url
    //이미지는 DsLayoutLoader여기서 받고 CCTV는 streamingBackFrame 여기서 할당받음
    member val ImageScreen: Mat= new Mat() with get, set 

type DsLayoutLoader(dsSystem:DsSystem, runtimeDir:string) =
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
        let chs = dsSystem.LayoutInfos.DistinctBy(fun f->f.ChannelName+f.Path).ToList()
        chs.ForEach(fun info ->
                let screenImage = ServerImage(info.ChannelName, info.ScreenType, info.Path)
                screenImage.ImageScreen <-
                    if info.ScreenType = ScreenType.IMAGE 
                    then 
                        let imgPath = PathManager.combineFullPathFile([|runtimeDir;$"{info.ChannelName}.jpg"|])
                        let img = File.ReadAllBytes(imgPath) |> OpenCVUtils.ByteArrayToMat 
                        OpenCVUtils.ResizeImage(img, _StreamSize.Width, _StreamSize.Height)
                    else new Mat()

                serverImages.Add(screenImage) |>ignore          
        )
                

    member x.DsSystem = dsSystem
    member x.LayoutInfos = dsSystem.LayoutInfos
    member x.GetBackFrameOrNotNullUpdate(ch:string, frame:Mat option) = getBackFrameOrNotNullUpdate(ch, frame) 

    member x.GetViewTypeList() =  Enum.GetNames(typeof<ViewType>) 

    member x.GetServerChannels()       = serverImages.Select(fun f->f.ChannelName).Order()
    member x.GetUrl(ch:string)         = serverImages.First(fun f-> f.ChannelName = ch).URL
    member x.GetChannelName(ch:string) = serverImages.First(fun f-> f.ChannelName = ch).ChannelName
    member x.GetScreenType(ch:string)  = serverImages.First(fun f-> f.ChannelName = ch).ScreenType
    member x.GetImage(ch:string)       = serverImages.First(fun f-> f.ChannelName = ch).ImageScreen

    