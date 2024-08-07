namespace Engine.Import.Office

open System.Linq
open Engine.Core
open Microsoft.FSharp.Core
open Dual.Common.Core.FS
open System.Text.RegularExpressions

[<AutoOpen>]
module PptNodeUtilModule =

        let trimNewLine (text: string) = text.Replace("\n", "")
        let trimSpace (text: string) = text.TrimStart(' ').TrimEnd(' ')
        let trimSpaceNewLine (text: string) = text |> trimSpace |> trimNewLine
        let trimStartEndSeq (texts: string seq) = texts |> Seq.map trimSpace


        let getPostParam(param:TaskDevParam option) (prefix:string)=
            if param.IsNone then ""
            else
                let param = param |> Option.get
                match param.DevValue, param.DevTime with
                | Some v, None -> $"{prefix}{v}"
                | Some v, Some t -> $"{prefix}{v}_{t}ms"
                | None, Some t -> $"{prefix}{t}ms"
                | None, None -> $""

        let getJobNameWithTaskDevParaIO(jobFqdn:string seq, taskDevParamIO:TaskDevParamIO) =
            let newJob =
                let inParaText  = getPostParam taskDevParamIO.InParam "IN"
                let outParaText = getPostParam taskDevParamIO.OutParam "OUT"


                if inParaText = "" && outParaText = "" //둘다 없는경우
                then
                    jobFqdn

                elif inParaText = "" && outParaText <> ""  //OUT 만 있는경우
                then
                    jobFqdn.SkipLast(1).Append( $"{jobFqdn.Last()}({outParaText})").ToArray()

                elif inParaText <> "" && outParaText = ""  //IN 만 있는경우
                then
                    jobFqdn.SkipLast(1).Append( $"{jobFqdn.Last()}({inParaText})").ToArray()

                else //둘다 있는경우
                    jobFqdn.SkipLast(1).Append( $"{jobFqdn.Last()}({inParaText}_{outParaText})").ToArray()
            newJob


        let getJobNameWithJobParam(jobFqdn:string seq, jobParam:JobParam) =
            let jobParamText =  jobParam.ToText()
            match jobParamText with
            | "" -> jobFqdn
            |_   ->
                    jobFqdn.SkipLast(1).Append( $"{jobFqdn.Last()}[{jobParamText}]").ToArray()


        let getNodeTaskDevParam (shape:Shape, name:string, iPage:int, isRoot, nodeType) =
            let error()  = $"{name} 입출력 규격을 확인하세요. \r\nDevice.Api(입력, 출력) 규격 입니다. \r\n기본예시(300,500) 입력생략(-,500) 출력생략(300, -)"
            try
                let getParam x =
                        if x = TextSkip then
                            "" |> getTaskDevParam |> snd
                        else
                            match getTextValueNType x with
                            | Some (v, t) ->
                                //if t = DuINT32 then  //ppt는 정수입력은 기본 int16으로 처리
                                //    $":{v}s" |> getTaskDevParam |> snd
                                //else
                                    $":{x}" |> getTaskDevParam |> snd
                            | None -> failwithf $"{x} 입력규격을 확인하세요"

                let func = GetLastParenthesesContents(name) |> trimSpaceNewLine
                if func.Contains(",") then

                    let inFunc, outFunc =
                        func.Split(",").Head().Replace(TextJobNegative, "") |> trimSpaceNewLine, //JobNegative 은 jobParam에서 다시 처리
                        func.Split(",").Last() |> trimSpaceNewLine
                    TaskDevParamIO((getParam inFunc)|>Some, (getParam outFunc)|>Some)
                else
                    if isRoot || nodeType = AUTOPRE //생략 규격 입력시에 Root/AUTOPRE 는 조건으로 Real내부는 출력으로 인식
                    then
                        TaskDevParamIO((getParam func)|>Some, (defaultTaskDevParam())|>Some)
                    else
                        TaskDevParamIO((defaultTaskDevParam())|>Some, (getParam func)|>Some)
            with _ ->
                shape.ErrorName((error()), iPage)

        let getTrimName(shape:Shape, nameTrim:string) =
            match GetSquareBrackets(nameTrim, false) with
            | Some text ->
                let pureName = nameTrim |> GetBracketsRemoveName |> trimSpaceNewLine
                if shape.IsHomePlate() then pureName //AA [xxx ~ yyy]
                else $"{pureName}[{text}]"   //AA[4]
            | None -> nameTrim

        let getNodeType(shape:Shape, name:string, iPage:int) =

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



