module DsStreamingFrontModule

open System
open System.Linq
open System.Drawing
open Engine.Core
open OxyImgUtils
open DsLayoutLoaderModule
open OpenCVUtils


let getViewType (viewtype:string) =
    match viewtype with
    | "Normal" -> ViewType.Normal
    | "Error" -> ViewType.Error
    | _ -> ViewType.Normal

let mutable cnt = 0
let getErrorImage (f:InfoDevice, xywh:Xywh) =
    //f.ErrorMessages.Clear()  
    //f.ErrorMessages.Add("Timeout Err")     
    //f.ErrorMessages.Add("Sensor Err")     
    let errText = String.Join("\n", f.ErrorMessages)
    let backColor, text = 
                    if f.ErrorMessages.Any() 
                    then Color.OrangeRed,  $"{errText}"
                    else Color.Green,  "OK"
    createBoxImage(f.Name,  text, rect xywh, backColor)

let getChartImage (f:InfoDevice, xywh:Xywh) =
    //let rand = Random()
    //f.GoingCount <- rand.Next(10,500)
    //f.ErrorCount <- rand.Next(0,55)
    createPieChartImage(f.Name, rect xywh, f.GoingCount, f.ErrorCount)

let getErrorImages (imgInfos:(InfoDevice*Xywh) seq) =  imgInfos |> Seq.map(getErrorImage)
let getChartImages (imgInfos:(InfoDevice*Xywh) seq) = 
   
    let normal = imgInfos.Where(fun (d,_)->d.ErrorMessages.Any()) |> Seq.map(getErrorImage) |> Seq.toList
    let error = imgInfos.Where(fun (d,_)->not(d.ErrorMessages.Any())) |> Seq.map(getChartImage) |> Seq.toList
    normal @ error

let getFrontImage(viewType, imgInfos) =
    let img =
        match viewType with
        | ViewType.Normal -> imgInfos |> getChartImages  |> OpenCVUtils.CombineImages _StreamSize
        | ViewType.Error  -> imgInfos |> getErrorImages  |> OpenCVUtils.CombineImages _StreamSize
        | _ -> failwithf $"GetFrontImage {viewType}: error Type"
    img
