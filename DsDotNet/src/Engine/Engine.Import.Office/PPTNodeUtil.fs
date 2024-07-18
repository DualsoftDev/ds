namespace Engine.Import.Office

open DocumentFormat.OpenXml.Packaging
open System
open System.Linq
open DocumentFormat.OpenXml
open Engine.Core
open System.Collections.Generic
open Microsoft.FSharp.Core
open Dual.Common.Core.FS
open System.Text.RegularExpressions

[<AutoOpen>]
module PPTNodeUtilModule =

        let trimNewLine (text: string) = text.Replace("\n", "")
        let trimSpace (text: string) = text.TrimStart(' ').TrimEnd(' ')
        let trimSpaceNewLine (text: string) = text |> trimSpace |> trimNewLine
        let trimStartEndSeq (texts: string seq) = texts |> Seq.map trimSpace

 
        let getPostParam(param:DevParam) =
            match param.DevValue, param.DevTime with 
            | Some v, None -> $"{v}"
            | Some v, Some t -> $"{v}_{t}ms"
            | None, Some t -> $"{t}ms"
            | None, None -> $""

        let getJobNameWithParams(jobFqdn:string seq, inParam:DevParam option, outParam:DevParam option) =
            if inParam.IsSome && outParam.IsNone then
                let post = getPostParam inParam.Value
                if post = "" 
                then jobFqdn 
                else jobFqdn.SkipLast(1).Append( $"{jobFqdn.Last()}(IN{post})").ToArray()

            elif inParam.IsSome && outParam.IsSome then
                let postIn = getPostParam inParam.Value
                let postOut = getPostParam outParam.Value
                if postIn = "" && postOut = "" 
                then jobFqdn
                else jobFqdn.SkipLast(1).Append( $"{jobFqdn.Last()}(IN{postIn}_OUT{postOut})").ToArray()  

            else jobFqdn

        //let getOperatorParam (shape:Shape, name:string, iPage:int) = 
        //    try
        //        let func = GetLastParenthesesContents(name) |> trimSpaceNewLine
        //        if func.Contains(",") then 
        //            failwithf $"{name} 입출력 규격을 확인하세요. \r\nDevice.Api(입력) 규격 입니다"
        //        $"{'-'}:{func}" |> getDevParam |> snd
        //    with ex ->
        //        shape.ErrorName(ex.Message, iPage)

        let getNodeDevParam (shape:Shape, name:string, iPage:int, target) = 
            let error()  = $"{name} 입출력 규격을 확인하세요. \r\nDevice.Api(입력, 출력) 규격 입니다. \r\n기본예시(300,500) 입력생략(-,500) 출력생략(300, -)"
            try
                let getParam x = 
                        if x = TextSkip then 
                            "" |> getDevParam |> snd
                        else
                            match getTextValueNType x with
                            | Some (v, t) ->
                                if t = DuINT32 && target = XGK then  //xgk는 기본 word 규격인 us로 변환
                                    $":{v}s" |> getDevParam |> snd
                                else
                                    $":{x}" |> getDevParam |> snd
                            | None -> failwithf $"{x} 입력규격을 확인하세요"

                let func = GetLastParenthesesContents(name) |> trimSpaceNewLine
                if func.Contains(",") then 

                    let inFunc, outFunc =
                        func.Split(",").Head() |> trimSpaceNewLine,
                        func.Split(",").Last() |> trimSpaceNewLine

                    Some(getParam inFunc), Some(getParam outFunc)

                else 
                    Some(getParam func), Some(defaultDevParam())
            with _ ->
                shape.ErrorName((error()), iPage)

        let getTrimName(shape:Shape) (nameTrim:string) =
            match GetSquareBrackets(nameTrim, false) with
            | Some text -> 
                let pureName = nameTrim |> GetBracketsRemoveName |> trimSpaceNewLine
                if shape.IsHomePlate() then pureName //AA [xxx ~ yyy] 
                else $"{pureName}[{text}]"   //AA[4] 
            | None -> nameTrim

        let getNodeType(shape:Shape, iPage:int) = 
            let nameNfunc = GetBracketsRemoveName(shape.InnerText)
            let name = GetLastParenthesesReplaceName(nameNfunc, "")

            match shape with
            | s when s.IsRectangle() ->
                if name.Contains(".") then REALExF else REAL
            | s when s.IsHomePlate() -> IF_DEVICE
            | s when s.IsFoldedCornerPlate() -> OPEN_EXSYS_CALL
            | s when s.IsFoldedCornerRound() -> COPY_DEV
            | s when s.IsEllipse() ->
                if s.IsDashShape() then AUTOPRE else CALL
            | s when s.IsBevelShapePlate() -> LAMP
            | s when s.IsBevelShapeRound() -> BUTTON
            | s when s.IsBevelShapeMaxRound() -> CONDITION
            | s when s.IsLayout() -> shape.ErrorName(ErrID._62, iPage)
            
            | _ ->
                failWithLog ErrID._1


        let updateRealTime (contents: string) =
            
            let parseSeconds (timeStr: string) : float option =
                if timeStr = TextSkip 
                then None
                else
                    let timeStr =timeStr.ToLower().Trim()
                    let msPattern = @"(\d+(\.\d+)?)ms"
                    let secPattern = @"(\d+(\.\d+)?)sec"
                    let minPattern = @"(\d+(\.\d+)?)min"
                    let defaultPattern = @"(\d+(\.\d+))"

                    let matchRegex pattern =
                        let m = Regex.Match(timeStr, pattern)
                        if m.Success then
                            let value = m.Groups.[1].Value |> float
                            Some value
                        else None

                    match matchRegex msPattern with
                    | Some ms -> Some (ms / 1000.0)
                    | None ->
                        match matchRegex secPattern with
                        | Some sec -> Some sec
                        | None ->
                            match matchRegex minPattern with
                            | Some min -> Some (min * 60.0)
                            | None ->
                                // Default to seconds if no unit is specified
                                match matchRegex defaultPattern with
                                | Some sec -> Some sec
                                | None -> failWithLog $"{timeStr} Invalid time format"

            let parts = (GetLastParenthesesContents contents).Split(',')

            let goingT, delayT = 
                if parts.[0] = "" then None, None
                else
                    match parts.Length with
                    | 1 ->
                        let firstTime = parts.[0].Trim() |> parseSeconds
                        (firstTime, None)
                    | 2 ->
                        let firstTime = parts.[0].Trim() |> parseSeconds
                        let secondTime = parts.[1].Trim() |> parseSeconds
                        (firstTime, secondTime)
                    | _ -> (None, None)

            goingT, delayT
        

        let getBracketItems (name: string) =
                name.Split('[').Select(fun w -> w.Trim()).Where(fun w -> w <> "")
                |> Seq.map (fun f ->
                    match GetSquareBrackets("[" + f, true) with
                    | Some item -> GetBracketsRemoveName("[" + f.TrimEnd('\n')), item
                    | None -> GetBracketsRemoveName("[" + f.TrimEnd('\n')), "")



