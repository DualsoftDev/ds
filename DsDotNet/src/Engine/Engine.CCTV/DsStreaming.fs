module DsStreamingModule

open System.Collections.Generic
open System
open System.Net.WebSockets
open System.Threading
open Emgu.CV
open Emgu.CV.CvEnum
open DsLayoutLoaderModule
open OpenCVUtils
open System.Drawing


type ViewType =
    | Chart
    | Table
    
let getViewType (viewtype:string) =
    match viewtype with
    | "Chart" -> ViewType.Chart
    | "Table" -> ViewType.Table
    | _ -> ViewType.Table


[<AutoOpen>]
type DsStreaming() =
    let _delayFps = 1000 / 60
    let _delayCCTV = 1000 / 100
    let _webViewTypes = Dictionary<string, ViewType>()
    let _webStreamSet = Dictionary<string, byte[]>()
    let _backFrame = Dictionary<string, Mat>()
    let _dsLayoutLoader = DsLayoutLoader()

   
  
    let _lockBackFrame = obj()
    let getBackFrameOrNotNullUpdate(url:string, frame:Mat option)  =
        lock _lockBackFrame (fun () ->
            if frame.IsSome then
                _backFrame.[url] <- frame.Value
        
            _backFrame.[url]
        )

    let _lockWebViewTypes = obj()
    let updateOrGetWebViewTypes (url:string option, viewType:ViewType option)  =
        lock _lockWebViewTypes (fun () ->
            if viewType.IsSome then
                _webViewTypes.[url.Value] <- viewType.Value
            Seq.toList _webViewTypes
        )
       


    member x.ImageStreaming(webSocket:WebSocket, screenId, viewmodeName, ipPort) =
        async { 
            let viewKey = $"{screenId}:{ipPort}"
            try
                let url = _dsLayoutLoader.GetScreen(screenId).URL
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
    
   

    member x.StreamingBackFrame(url:string) =
        let cts = new CancellationTokenSource()
        let capture = new VideoCapture(url, VideoCapture.API.Ffmpeg)
        let backFrame = new Mat()

        Async.Start (async {
            while not cts.Token.IsCancellationRequested do
                capture.Read backFrame |> ignore
                getBackFrameOrNotNullUpdate(url, Some backFrame) |> ignore
                do! Async.Sleep(_delayCCTV)
        }, cancellationToken = cts.Token)
        |> ignore





    member x.GetFrontImage(viewType) =
        let r = Random()
        let dt = OpenCVUtils.GetSampleDataTable(r.Next(10, 30))

        let img =
            match viewType with
            | ViewType.Table -> OxyChart.visualizeImage().ToArray()
               //camTable.ConvertToImage([dt]) |> Seq.head
            | ViewType.Chart -> OxyChart.visualizeImage().ToArray()
               //camChart.ConvertToImage([dt], viewType) |> Seq.head
        img

    member x.StreamingFrontFrame() =
        let cts = new CancellationTokenSource()

        let streamingTask = async {
            let frontFrame = new Mat()
            let mixFrame = new Mat()
            while not cts.Token.IsCancellationRequested do
                let streamList = updateOrGetWebViewTypes(None, None)
                for kvp in streamList do
                    let item = kvp.Value
                    let url = _dsLayoutLoader.GetScreen(kvp.Key.Split(':').[0]).URL
                    if _backFrame.ContainsKey(url) then
                        let backFrame = getBackFrameOrNotNullUpdate(url, None) 
                        let frontImg = x.GetFrontImage(item) 
                        let backSize = backFrame.Size

                        CvInvoke.Imdecode(frontImg, ImreadModes.Color, frontFrame)
                        //frontFrame <- OpenCVUtils.ResizeImage(frontFrame, backSize.Width, backSize.Height)
                        let mixFrame = OpenCVUtils.AlphaBlend(frontFrame, new Point(0, 0), backFrame)
                        let compressedImage = OpenCVUtils.CompressImage mixFrame
                        _webStreamSet.[kvp.Key] <- compressedImage
                    do! Async.Sleep(5)
            frontFrame.Dispose()
            mixFrame.Dispose()
        }

        Async.Start(streamingTask, cancellationToken = cts.Token) |> ignore


    member x.Streaming(urls) =
        urls |> Seq.iter x.StreamingBackFrame
        x.StreamingFrontFrame()

