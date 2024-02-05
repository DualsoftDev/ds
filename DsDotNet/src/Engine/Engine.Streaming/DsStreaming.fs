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
        ClientGuid : string 
    }
    with member x.Key =  $"{x.ClientGuid};{x.ChannelName}";


[<AutoOpen>]
type DsStreaming(dsSystem:DsSystem, runtimeDir:string) =
    let _delayFps = 1000 / 15
    let _streamClients = Dictionary<WebSocket, StreamClient>()
    let _webStreamSet = Dictionary<string, byte[]>()
    let _dsl = new DsLayoutLoader(dsSystem, runtimeDir)

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
        showDevs.Select(fun d-> infos.First(fun i->i.Name = d.Name)
                              , d.ChannelPoints.First(fun kv-> kv.Key.Split(';')[0] = $"{chName}").Value)


    let streamingFrontFrame() =
        let cts = new CancellationTokenSource()

        let streamingTask = async {
            let mixFrame = new Mat()
            while not cts.Token.IsCancellationRequested do
                let streamList = updateOrGetWebViewTypes(None, None)
                for kvp in streamList do
                    let item = kvp.Value
                    let chName = item.ChannelName
                    let backFrame = _dsl.GetBackFrameOrNotNullUpdate(chName, None) 

                    let imgInfos = getImageInfos  chName 
                    let frontFrame = getFrontImage(item.ViewType, imgInfos) 

                    let mixFrame = OpenCVUtils.AlphaBlend(frontFrame, backFrame)
                    _webStreamSet.[item.Key] <- OpenCVUtils.CompressImage mixFrame
                    frontFrame.Dispose()
                    mixFrame.Dispose()

                do! Async.Sleep(_delayFps)

        }

        Async.Start(streamingTask, cancellationToken = cts.Token) |> ignore

    do
        _dsl.DsSystem.LayoutCCTVs 
        |> Seq.iter(fun (ch,url) -> streamingBackFrame (_dsl,ch,url))
        streamingFrontFrame()


    member x.DsLayout = _dsl
    member x.GetImageInfos (url:string) = getImageInfos url
    member x.ImageStreaming(webSocket:WebSocket, channelName:string, viewmodeName, clientGuid) =
        async { 
            try
                let viewType = getViewType(viewmodeName)

                let screenInfo = { 
                                    ClientGuid = clientGuid
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
                        
                        do! Async.Sleep(_delayFps) //ÃÊ´ç 15Àå Å¸°Ù
                with
                | ex -> 
                    Console.WriteLine($"Error in image streaming: {ex.Message}")
            finally
                Console.WriteLine($"\"Image streaming ended\" in image streaming: {clientGuid}")
                webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Image streaming ended", CancellationToken.None) |> Async.AwaitTask |> ignore
                _streamClients.Remove(webSocket) |> ignore
        }   |> Async.StartAsTask
    
   

