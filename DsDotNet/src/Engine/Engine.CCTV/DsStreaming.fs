module DsStreamingModule

open System.Collections.Generic
open System
open System.Linq
open System.Net.WebSockets
open System.Threading
open Emgu.CV
open Emgu.CV.CvEnum
open DsLayoutLoaderModule
open OpenCVUtils
open System.Drawing
open Engine.Info
open Engine.Core
open DsStreamingFrontModule
open DsStreamingBackModule



[<AutoOpen>]
type StreamClient = {
        ChannelName :string 
        ViewType : ViewType
        IpPort : string 
    }
    with member x.Key =  $"{x.ChannelName}:{x.IpPort}";


[<AutoOpen>]
type DsStreaming() =
    let _delayFps = 1000 / 60
    let _streamClients = Dictionary<WebSocket, StreamClient>()
    let _webStreamSet = Dictionary<string, byte[]>()
    let _dsl = new DsLayoutLoader()

    let _lockWebViewTypes = obj()
    let updateOrGetWebViewTypes (webSocket:WebSocket option, streamClient:StreamClient option)  =
        lock _lockWebViewTypes (fun () ->
            if streamClient.IsSome then
                _streamClients.[webSocket.Value] <- streamClient.Value
            Seq.toList _streamClients
        )

    let layoutDic =
        _dsl.LayoutInfos
        |> Seq.groupBy (fun f -> f.ChannelName)
        |> Seq.map (fun (g, values) -> g, values)
        |> dict

    let getImageInfos (chName:string) =
        let showDevs  = 
            layoutDic[chName]
                .Select(fun f-> _dsl.DsSystem.Devices.First(fun d->d.LoadedName = f.DeviceName))
        let infos = InfoPackageModuleExt.GetInfos(showDevs)
        showDevs.Select(fun d-> infos.First(fun i->i.Name = d.Name), d.ChannelPoints.First(fun (ch,xy)-> ch.Split(';')[0] = $"{chName}")|>snd)


    let streamingFrontFrame() =
        let cts = new CancellationTokenSource()

        let streamingTask = async {
            let mixFrame = new Mat()
            while not cts.Token.IsCancellationRequested do
                let streamList = updateOrGetWebViewTypes(None, None)
                for kvp in streamList do
                    let item = kvp.Value
                    let chName = item.ChannelName
                    if _dsl.ExistImageScreenData(chName) then
                        let backFrame = _dsl.GetBackFrame(chName, None) 

                        let imgInfos = getImageInfos  chName 
                        let frontFrame = getFrontImage(item.ViewType, imgInfos) 
                        let backSize = backFrame.Size

                        let frontFrameResize = OpenCVUtils.ResizeImage(frontFrame, backSize.Width, backSize.Height)
                        let mixFrame = OpenCVUtils.AlphaBlend(frontFrameResize, new Point(0, 0), backFrame)
                        let compressedImage = OpenCVUtils.CompressImage mixFrame
                        _webStreamSet.[item.Key] <- compressedImage
                    do! Async.Sleep(1)

            mixFrame.Dispose()
        }

        Async.Start(streamingTask, cancellationToken = cts.Token) |> ignore

    do
        _dsl.DsSystem.LayoutCCTVs 
        |> Seq.iter(fun (ch,url) -> streamingBackFrame (_dsl,ch,url))
        streamingFrontFrame()


    member x.DsLayout = _dsl
    member x.GetImageInfos (url:string) = getImageInfos url
    member x.ImageStreaming(webSocket:WebSocket, channelName:string, viewmodeName, ipPort) =
        async { 
            try
                let viewType = getViewType(viewmodeName)

                let screenInfo = { 
                                    IpPort = ipPort
                                    ChannelName = channelName
                                    ViewType = viewType 
                                 }
                let viewKey = screenInfo.Key;

                updateOrGetWebViewTypes(Some webSocket, Some screenInfo) |> ignore
                try 
                    while webSocket.State = WebSocketState.Open do
                        if _webStreamSet.ContainsKey(viewKey) then
                            let byteArray = _webStreamSet.[viewKey]
                            do! webSocket.SendAsync(new ArraySegment<byte>(byteArray), WebSocketMessageType.Binary, true, CancellationToken.None) |> Async.AwaitTask
                        do! Async.Sleep(_delayFps) 
                with
                | ex -> 
                    Console.WriteLine($"Error in image streaming: {ex.Message}")
            finally
                Console.WriteLine($"\"Image streaming ended\" in image streaming: {ipPort}")
                webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Image streaming ended", CancellationToken.None) |> Async.AwaitTask |> ignore
                _streamClients.Remove(webSocket) |> ignore
        }   |> Async.StartAsTask
    
   

