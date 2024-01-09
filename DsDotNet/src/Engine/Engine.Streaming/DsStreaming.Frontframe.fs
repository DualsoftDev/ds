module DsStreamingFrontModule

open System
open System.Linq
open System.Drawing
open Engine.Core
open OxyImgUtils
open DsLayoutLoaderModule
open OpenCVUtils


let _StreamFrontSize = Size(1920, 1080)

let getViewType (viewtype:string) =
    match viewtype with
    | "Normal" -> ViewType.Normal
    | "Error" -> ViewType.Error
    | _ -> ViewType.Normal


[<Obsolete("Remove test ErrorMessages test ....")>]
let getTableImage (imgInfos:(InfoDevice*Xywh) seq) =
    imgInfos.Select(fun (f, xywh)-> 
        f.ErrorMessages.Clear()  
        f.ErrorMessages.Add("출력 Timeout")     
        f.ErrorMessages.Add("센서 고장")     
        let errText = String.Join(", ", f.ErrorMessages)
        let backColor, text = 
                        if f.ErrorMessages.Any() 
                        then Color.OrangeRed,  $"{f.Name}\n{errText}"
                        else Color.Green,  f.Name
        createBoxImage(text, rect xywh, backColor)
    )   

                
[<Obsolete("Remove random test data....")>]
let getChartImage (imgInfos:(InfoDevice*Xywh) seq) = 
    imgInfos.Select(fun (f, xywh)->
        let rand = Random()
        f.GoingCount <- rand.Next(10,500)
        f.ErrorCount <- rand.Next(0,55)
        createPieChartImage(f.Name, rect xywh, f.GoingCount, f.ErrorCount)
    )   
            

let getFrontImage(viewType, imgInfos) =
    let img =
        match viewType with
        | ViewType.Normal -> imgInfos |> getChartImage  |> OpenCVUtils.CombineImages _StreamFrontSize
        | ViewType.Error  -> imgInfos |> getTableImage  |> OpenCVUtils.CombineImages _StreamFrontSize
        | _ -> failwithf $"GetFrontImage {viewType}: error Type"
    img
