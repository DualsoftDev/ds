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
type DsStreaming() =
    let _delayFps = 1000 / 60
    let _webViewTypes = Dictionary<string, ViewType>()
    let _webStreamSet = Dictionary<string, byte[]>()
    let _dsl = new DsLayoutLoader()

    let _lockWebViewTypes = obj()
    let updateOrGetWebViewTypes (url:string option, viewType:ViewType option)  =
        lock _lockWebViewTypes (fun () ->
            if viewType.IsSome then
                _webViewTypes.[url.Value] <- viewType.Value
            Seq.toList _webViewTypes
        )

    let layoutDic =
        _dsl.LayoutInfos
        |> Seq.groupBy (fun f -> f.Path)
        |> Seq.map (fun (g, values) -> g, values)
        |> dict

    let getImageInfos (url:string) =
        let showDevs  = 
            layoutDic[url]
                .Select(fun f-> _dsl.DsSystem.Devices.First(fun d->d.LoadedName = f.DeviceName))
        let infos = InfoPackageModuleExt.GetInfos(showDevs)
        showDevs.Select(fun d-> infos.First(fun i->i.Name = d.Name), d.Xywh)


    let streamingFrontFrame() =
        let cts = new CancellationTokenSource()

        let streamingTask = async {
            let frontFrame = new Mat()
            let mixFrame = new Mat()
            while not cts.Token.IsCancellationRequested do
                let streamList = updateOrGetWebViewTypes(None, None)
                for kvp in streamList do
                    let item = kvp.Value
                    let url = _dsl.GetScreen(kvp.Key.Split(':').[0]).URL
                    if _backFrame.ContainsKey(url) then
                        let backFrame = getBackFrameOrNotNullUpdate(url, None) 

                        let imgInfos = getImageInfos url
                        let frontFrame = getFrontImage(item, imgInfos) 
                        let backSize = backFrame.Size

                        //CvInvoke.Imdecode(frontImg, ImreadModes.Color, frontFrame)
                        //frontFrame <- OpenCVUtils.ResizeImage(frontFrame, backSize.Width, backSize.Height)
                        let mixFrame = OpenCVUtils.AlphaBlend(frontFrame, new Point(0, 0), backFrame)
                        let compressedImage = OpenCVUtils.CompressImage mixFrame
                        _webStreamSet.[kvp.Key] <- compressedImage
                    do! Async.Sleep(1)
            frontFrame.Dispose()
            mixFrame.Dispose()
        }

        Async.Start(streamingTask, cancellationToken = cts.Token) |> ignore

    do
        _dsl.DsSystem.LayoutChannels |> Seq.iter streamingBackFrame
        streamingFrontFrame()


    member x.DsLayout = _dsl
    member x.GetImageInfos (url:string) = getImageInfos url
    member x.ImageStreaming(webSocket:WebSocket, screenId, viewmodeName, ipPort) =
        async { 
            let viewKey = $"{screenId}:{ipPort}"
            try
                let url = _dsl.GetScreen(screenId).URL
                let viewKey = $"{screenId}:{ipPort}";
                let viewType = getViewType(viewmodeName)
                updateOrGetWebViewTypes(Some viewKey, Some viewType) |> ignore
            
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
                webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Image streaming ended", CancellationToken.None) |> Async.AwaitTask |> ignore
                Console.WriteLine($"\"Image streaming ended\" in image streaming: {ipPort}")
                _webViewTypes.Remove(viewKey) |> ignore
        }   |> Async.StartAsTask
    
   

