module DsStreamingBackModule

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
open OxyImgUtils


let _delayCCTV = 1000 / 100
let _backFrame = Dictionary<string, Mat>()

let _lockBackFrame = obj()
let getBackFrameOrNotNullUpdate(url:string, frame:Mat option)  =
    lock _lockBackFrame (fun () ->
        if frame.IsSome then
            _backFrame.[url] <- frame.Value
        
        _backFrame.[url]
    )


let streamingBackFrame(url:string) =
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