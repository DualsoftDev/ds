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


let _delayCCTV = 1000 / 60


let streamingBackFrame(loader:DsLayoutLoader, channelName:string, url:string) =
    let cts = new CancellationTokenSource()
    let capture = new VideoCapture(url, VideoCapture.API.Ffmpeg)

    Async.Start (async {
        while not cts.Token.IsCancellationRequested do
            let backFrame = new Mat()
            
            if  capture.Read backFrame then

                let backFrameResize = OpenCVUtils.ResizeImage(backFrame, _StreamSize.Width, _StreamSize.Height)

                loader.GetBackFrameOrNotNullUpdate(channelName, Some backFrameResize) |> ignore

                backFrame.Dispose()

            do! Async.Sleep(_delayCCTV)
    }, cancellationToken = cts.Token)
    |> ignore

