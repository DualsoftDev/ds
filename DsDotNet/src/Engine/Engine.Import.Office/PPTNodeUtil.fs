namespace Engine.Import.Office

open System.Linq
open Engine.Core
open Microsoft.FSharp.Core
open Dual.Common.Core.FS
open Dual.Common.Base.FS
open System.Text.RegularExpressions
open System

[<AutoOpen>]
module PptNodeUtilModule =

        let trimNewLine (text: string) = text.Replace("\n", "")
        let trimSpace (text: string) = text.TrimStart(' ').TrimEnd(' ')
        let trimSpaceNewLine (text: string) = text |> trimSpace |> trimNewLine
        let trimStartEndSeq (texts: string seq) = texts |> Seq.map trimSpace

        //api 이름 구분용 totext
        let getPostParam(param:TaskDevParam option) (prefix:string)=
            if param.IsNone then ""
            else
                let param = param |> Option.get
                if param.IsDefaultParam then ""
                else 
                    if param.ValueParam.IsDefaultValue 
                    then
                        match param.DevTime with
                        | Some t -> $"{prefix}{t}ms"
                        | None -> $""
                    else 
                        match param.DevTime with
                        | Some t -> $"{prefix}{param.ValueParam.ToText()}{t}ms"
                        | None -> $"{prefix}{param.ValueParam.ToText()}"
                        

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
            let error(msg)  = $"{msg} \r\n{name} 입출력 규격을 확인하세요. \r\nDevice.Api(입력, 출력) 규격 입니다. \r\n기본예시(300,500) 입력생략(-,500) 출력생략(300, -)"
            try
                let getParam x =
                        if x = TextSkip then
                            defaultTaskDevParam()
                        else
                            getTaskDevParam x

                            //match  with
                            //| Some vp ->
                            //    //if t = DuINT32 then  //ppt는 정수입력은 기본 int16으로 처리
                            //    //    $":{v}s" |> getAddressTaskDevParam |> snd
                            //    //else
                            //        if vp.IsDefaultValue then
                            //            $":True" |> getAddressTaskDevParam |> snd
                            //        else
                            //            $":{vp.ToText()}" |> getAddressTaskDevParam |> snd
                            //| None -> failwithf $"{x} 입력규격을 확인하세요"

                let func = GetLastParenthesesContents(name) |> trimSpaceNewLine
                if func.Contains(",") then

                    let inFunc, outFunc =
                        func.Split(",").Head().Replace(TextJobNegative, "") |> trimSpaceNewLine, //JobNegative 은 jobParam에서 다시 처리
                        func.Split(",").Last() |> trimSpaceNewLine
                    TaskDevParamIO((getParam inFunc)|>Some, (getParam outFunc)|>Some)
                else
                    let param = getParam func
                    if isRoot || nodeType = AUTOPRE //생략 규격 입력시에 Root/AUTOPRE 는 조건으로 Real내부는 출력으로 인식
                    then
                        TaskDevParamIO(param|>Some, (defaultTaskDevParam())|>Some)
                    else
                        if param.ValueParam.IsRangeValue
                        then 
                            failwithlog $"RangeValue은 입력규격만 가능합니다."    

                        TaskDevParamIO((defaultTaskDevParam())|>Some, param|>Some)
            with ex ->
                shape.ErrorName(error(ex.Message), iPage)

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
            | s when s.IsBevelShapeMaxRound() -> CONDITIONorAction
            | s when s.IsLayout() -> shape.ErrorName(ErrID._62, iPage)

            | _ ->
                failWithLog ErrID._1

        let updateRepeatCount (contents: string) =
            let parseCount (txt: string) =
                match UInt32.TryParse txt with
                | true, count when count > 0u -> Some (int count)
                | _ -> None

            let parts = 
                (GetLastParenthesesContents contents).Split(',')
                |> Array.choose parseCount

            // Validate and return the repeat count
            match parts with
            | [| singleCount |] -> Some singleCount
            | [| |] -> None
            | _ -> failWithLog "Only one repeat count entry is allowed"

    
        let updateRealTime (contents: string) =
            let parseSeconds (timeStr: string) : float option =
                let timeStr = timeStr.ToLower().Trim()
                let msPattern = @"(\d+(\.\d+)?)ms"
                let secPattern = @"(\d+(\.\d+)?)sec"
                let minPattern = @"(\d+(\.\d+)?)min"

                let matchRegex pattern =
                    let m = Regex.Match(timeStr, pattern)
                    if m.Success then Some (m.Groups.[1].Value |> float) else None

                match matchRegex msPattern with
                | Some ms when ms < 10.0 -> failWithLog $"{timeStr} Invalid time format: must be 10ms or greater"
                | Some ms -> Some (ms / 1000.0)
                | None ->
                    match matchRegex secPattern with
                    | Some sec -> Some sec
                    | None ->
                        match matchRegex minPattern with
                        | Some min -> Some (min * 60.0)
                        | None -> None
                           
            let parts = (GetLastParenthesesContents contents).Split(',') |> Seq.choose parseSeconds
    
            // Check for invalid multiple time entries
            if parts.length() > 1 then failWithLog "Only one time entry is allowed"

            // Parse the single time entry if available
            let goingSec =
                if parts.IsEmpty then None
                else parts.Head() |> Some
    
            // Ensure parsed time is in 1ms increments
            match goingSec with
            | Some t when (t*1000.0) % 10.0 <> 0.0 -> failWithLog $"{contents} Invalid time format: must be in increments of 10ms"
            | _ -> goingSec

        let getBracketItems (name: string) =
                name.Split('[').Select(fun w -> w.Trim()).Where(fun w -> w <> "")
                |> Seq.map (fun f ->
                    match GetSquareBrackets("[" + f, true) with
                    | Some item -> GetBracketsRemoveName("[" + f.TrimEnd('\n')), item
                    | None -> GetBracketsRemoveName("[" + f.TrimEnd('\n')), "")


        let getPureNFunction (fullName: string, isInput:bool) =
            let pureName = GetLastParenthesesReplaceName(fullName, "")
            let funcName = GetLastParenthesesContents(fullName) |> trimSpaceNewLine

            let devParamIO =
                if funcName <> ""
                then
                    let devParam = getTaskDevParam (funcName)
                    if isInput 
                    then
                        TaskDevParamIO(Some devParam, None)
                    else 
                        TaskDevParamIO(None, Some devParam)
                else
                    defaultTaskDevParamIO()

            pureName, devParamIO