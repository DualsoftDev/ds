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

        ////api 이름 구분용 totext
        //let getPostParam(param:ValueParam option) (prefix:string)=
        //    if param.IsNone then ""
        //    else
        //        let param = param |> Option.get
        //        if param.IsDefault then ""
        //        else 
        //            if param.ValueParam.IsDefaultValue 
        //            then ""
        //            else $"{prefix}{param.ValueParam.ToText()}"
                        

        //let getJobNameWithTaskDevParaIO(jobFqdn:string seq, taskDevParamIO:TaskDevParamIO) =
        //    let newJob =
        //        let inParaText  = getPostParam taskDevParamIO.InParam "IN"
        //        let outParaText = getPostParam taskDevParamIO.OutParam "OUT"


        //        if inParaText = "" && outParaText = "" //둘다 없는경우
        //        then
        //            jobFqdn

        //        elif inParaText = "" && outParaText <> ""  //OUT 만 있는경우
        //        then
        //            jobFqdn.SkipLast(1).Append( $"{jobFqdn.Last()}({outParaText})").ToArray()

        //        elif inParaText <> "" && outParaText = ""  //IN 만 있는경우
        //        then
        //            jobFqdn.SkipLast(1).Append( $"{jobFqdn.Last()}({inParaText})").ToArray()

        //        else //둘다 있는경우
        //            jobFqdn.SkipLast(1).Append( $"{jobFqdn.Last()}({inParaText}_{outParaText})").ToArray()
        //    newJob


        let getJobNameWithJobParam(jobFqdn:string seq, jobDevParam:JobDevParam) =
            let jobParamText =  jobDevParam.ToText()
            match jobParamText with
            | "" -> jobFqdn
            |_   ->
                    jobFqdn.SkipLast(1).Append( $"{jobFqdn.Last()}[{jobParamText}]").ToArray()


        let getNodeValueParam (shape:Shape, name:string, iPage:int, isRoot, nodeType) =
            let error(msg)  = $"{msg} \r\n{name} 입출력 규격을 확인하세요. \r\nDevice.Api(입력, 출력) 규격 입니다. \r\n기본예시(300,500) 입력생략(-,500) 출력생략(300, -)"
            try
                let getParam x =
                        if x = TextSkip then
                            defaultValueParam() 
                        else
                            createValueParam x 
                            

                let func = GetLastParenthesesContents(name) |> trimSpaceNewLine
                if func.Contains(",") then

                    let inFunc, outFunc =
                        func.Split(",").Head().Replace(TextJobNegative, "") |> trimSpaceNewLine, //JobNegative 은 jobDevParam에서 다시 처리
                        func.Split(",").Last() |> trimSpaceNewLine
                    ((getParam inFunc), (getParam outFunc))
                else
                    let param = getParam func
                    if isRoot || nodeType = AUTOPRE //생략 규격 입력시에 Root/AUTOPRE 는 조건으로 Real내부는 출력으로 인식
                    then
                        (param, defaultValueParam())
                    else
                        if param.IsRangeValue
                        then 
                            failwithlog $"RangeValue은 입력규격만 가능합니다."    

                        (defaultValueParam(), param)

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

        let getRepeatCount (contents: string) =
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


        let getCallTime (contents: string) : CallTime  =
            // CallTime 객체 생성 및 값 설정
            let CallTime = CallTime()
            CallTime.TimeOut    <- parseUIntMSec contents TextMAX
            CallTime.DelayCheck <- parseUIntMSec contents TextCHK

            CallTime

        let getBracketItems (name: string) =
                name.Split('[').Select(fun w -> w.Trim()).Where(fun w -> w <> "")
                |> Seq.map (fun f ->
                    match GetSquareBrackets("[" + f, true) with
                    | Some item -> GetBracketsRemoveName("[" + f.TrimEnd('\n')), item
                    | None -> GetBracketsRemoveName("[" + f.TrimEnd('\n')), "")

                    
 
        let getPureNValueParam (fullName: string, isInput:bool) =
            let pureName = GetLastParenthesesRemoveName(fullName) |> trimSpaceNewLine
            let funcName = GetLastParenthesesContents(fullName) |> trimSpaceNewLine

            let devParamIO =
                if funcName <> ""
                then
                    let valParam = createValueParam (funcName)
                    if isInput 
                    then
                        ValueParamIO( valParam, defaultValueParam())
                    else 
                        ValueParamIO(defaultValueParam(), valParam)
                else
                    defaultValueParamIO()

            pureName, devParamIO