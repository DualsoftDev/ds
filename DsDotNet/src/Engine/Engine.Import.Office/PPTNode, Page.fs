namespace Engine.Import.Office

open DocumentFormat.OpenXml.Packaging
open System
open System.Linq
open DocumentFormat.OpenXml
open Engine.Core
open System.Collections.Generic
open Microsoft.FSharp.Core
open Dual.Common.Core.FS


[<AutoOpen>]
module PptNodeModule =

    type PptPage(slidePart: SlidePart, iPage: int, bShow: bool) =
        member x.PageNum = iPage
        member x.SlidePart = slidePart
        member x.IsUsing = bShow
        member x.Title = slidePart.PageTitle()

    let private nameNFunc(shape:Shape, macros:MasterPageMacro seq, iPage: int) =
        let mutable macroUpdateName =
            shape.InnerText.Replace("”", "\"").Replace("“", "\"")
        macros
            .Where(fun m->m.Page = iPage)
            .Iter(fun m-> macroUpdateName <- macroUpdateName.Replace($"{m.Macro}", $"{m.MacroRelace}"))

        macroUpdateName|> GetHeadBracketRemoveName |> trimSpaceNewLine //ppt “ ” 입력 호환

    type PptNode private(
        shape: Presentation.Shape, iPage: int, pageTitle: string, slieSize: int * int, isHeadPage: bool, macros:MasterPageMacro seq
        , copySystems        : Dictionary<string, string> //copyName, orgiName
        , safeties           : HashSet<string>
        , autoPres           : HashSet<string[]>  //callFqdn
        , jobInfos           : Dictionary<string, HashSet<string>> // jobBase, api SystemNames
        , btnHeadPageDefs    : Dictionary<string, BtnType>
        , btnDefs            : Dictionary<string, BtnType>
        , lampHeadPageDefs   : Dictionary<string, LampType>
        , lampDefs           : Dictionary<string, LampType>
        , condiHeadPageDefs  : Dictionary<string, ConditionType>
        , condiDefs          : Dictionary<string, ConditionType>
        , actionHeadPageDefs : Dictionary<string, ActionType>
        , actionDefs         : Dictionary<string, ActionType>

        , nodeType          : NodeType

        , ifName            : string
        , rootNode          : bool option
        //, taskDevParam      : TaskDevParamIO
        , jobDevParam       : JobDevParam
        , CallTime           : CallTime option
        , valueParamIO      : ValueParamIO 
        , ifTX              : string
        , ifRX              : string
        , realGoingTime     : CountUnitType option
        , realRepeatCnt     : CountUnitType option
        , name              : string
    ) =

        let mutable rootNode = rootNode
        //let mutable taskDevParam = taskDevParam


        member x.PageNum = iPage
        member x.Shape = shape
        member x.CopySys = copySystems
        member x.JobCallNames = jobInfos.Keys
        member x.RealFinished = shape.IsUnderlined()
        member x.RealNoTrans = if nodeType.IsReal then shape.IsStrikethrough() else failWithLog $"err: {name}RealNoTrans is not real Type"
        member x.DisableCall = if nodeType.IsCall then shape.IsStrikethrough() else failWithLog $"err: {name}CallSkipCoin is not call Type"

        member x.Safeties = safeties
        member x.AutoPres = autoPres

        member x.RealGoingTime = realGoingTime
        member x.RealRepeatCnt = realRepeatCnt
        member x.IfName = ifName
        member x.IfTX = ifTX
        member x.IfRX = ifRX

        member x.NodeType       = nodeType

        member x.PageTitle      = pageTitle
        member x.Position       = shape.GetPosition(slieSize)
        member x.OperatorName   = name
        member x.CommandName    = name
        member x.IsCall         = nodeType = CALL
        member x.IsRootNode     = rootNode
        member x.IsFunction     = x.IsCall && not(name.Contains("."))
        //member x.TaskDevParam   = taskDevParam
        member x.JobParam       = jobDevParam
        member x.ValueParamIO   = valueParamIO

        //member x.TaskDevParaIn  = taskDevParam.InParam
        //member x.TaskDevParaOut = taskDevParam.OutParam

        member x.UpdateRealProperty(real: Real) =
            match realGoingTime with
            | Some newValue ->
                match real.DsTime.AVG with
                | Some currentValue when currentValue <> newValue ->
                    shape.ErrorName(ErrID._76, iPage)
                | _ -> real.DsTime.AVG <-(Some newValue)
            | None -> ()

            match realRepeatCnt with
            | Some newValue ->
                match real.RepeatCount with
                | Some currentValue when currentValue <> newValue ->
                    shape.ErrorName(ErrID._84, iPage)
                | _ -> real.RepeatCount <-(Some newValue)
            | None -> ()

            if real.Finished = true && x.RealFinished = false then  //이미 설정을 true 하고 다른데서 변경
                shape.ErrorName(ErrID._77, iPage)
            if real.NoTransData = true && x.RealNoTrans = false then //이미 설정을 true 하고 다른데서 변경
                shape.ErrorName(ErrID._77, iPage)

            real.Finished <- x.RealFinished
            real.NoTransData <- x.RealNoTrans

        member x.Job = x.JobPure.ToArray()
            //getJobNameWithTaskDevParaIO(x.JobPure, taskDevParam).ToArray()

        member x.JobWithJobPara =
            getJobNameWithJobParam(x.Job, jobDevParam).ToArray()

        member x.UpdateNodeRoot(isRoot: bool) =
            rootNode <- Some isRoot
            //if nodeType = CALL || nodeType = AUTOPRE then
            //    let hasTaskDevParam = GetLastParenthesesReplaceName(nameNFunc(shape, macros, iPage), "") <> nameNFunc(shape, macros, iPage)
            //    if name.Contains(".")  //isDevCall
            //    then
            //        if hasTaskDevParam then
            //            let tPara = getNodeTaskDevParam (shape, nameNFunc(shape, macros, iPage), iPage, isRoot , nodeType)
            //            ValueParamIO <- tPara

            //        else
            //            if isRoot then
            //                let inPara = createTaskDevParam  None None  |> Some
            //                taskDevParam <-TaskDevParamIO(inPara, None)

            //            elif nodeType = AUTOPRE then
            //                taskDevParam <- createTaskDevParaIOInTrue()
            //    else
            //        if hasTaskDevParam then
            //            failWithLog $"{name} 'TaskDevParam' error"

        member x.UpdateCallProperty(call: Call) =
            call.Disabled <- x.DisableCall
            if CallTime.IsSome then
                call.CallTime <- CallTime.Value

        member x.JobPure : string seq =
            if not (nodeType = CALL || nodeType = AUTOPRE) then
                shape.ErrorName($"CallName not support {nodeType}({name}) type", iPage)

            let parts = GetLastParenthesesRemoveName(name).Split('.')
            if parts.Contains("")
            then
                shape.ErrorName("이름 규격을 확인하세요", iPage)

            match parts.Length with
            | 2 ->
                let jobBody =
                    [ pageTitle
                      parts.[0] |> TrimSpace |> GetBracketsRemoveName |> TrimSpace]
                let api = GetLastParenthesesRemoveName(parts.[1] |> TrimSpace) |> TrimSpace
                jobBody.Append api

            | 3 ->
                let jobBody =
                    [ parts.[0] |> TrimSpace
                      parts.[1] |> TrimSpace |> GetBracketsRemoveName |> TrimSpace]
                let api = GetLastParenthesesRemoveName(parts.[2] |> TrimSpace)|> TrimSpace
                jobBody.Append api

            | _ ->
                shape.ErrorShape("Action이름 규격을 확인하세요.", iPage)



        member x.DevName = $"{x.Job.Head()}{TextDeviceSplit}{x.Job.Skip(1).Head()}"
        member x.ApiName = x.Job.Last()
        member x.ApiPureName = x.JobPure.Last()

        member x.FlowName =  pageTitle
        member x.IsAlias: bool = x.Alias.IsSome
        member val Alias: PptNode option = None with get, set
        member val Id = shape.GetId()
        member val Key = Objkey(iPage, shape.GetId())
        member val Name = name with get, set
        member val AliasNumber: int = 0 with get, set
        member val ButtonHeadPageDefs  = btnHeadPageDefs
        member val ButtonDefs          = btnDefs
        member val LampHeadPageDefs    = lampHeadPageDefs
        member val LampDefs            = lampDefs
        member val CondiHeadPageDefs   = condiHeadPageDefs
        member val CondiDefs           = condiDefs
        member val ActionHeadPageDefs  = actionHeadPageDefs
        member val ActionDefs          = actionDefs
        member x.GetRectangle(slideSize: int * int) = shape.GetPosition(slideSize)


    type PptNode with
        static member Create(shape: Presentation.Shape, iPage: int, pageTitle: string, slieSize: int * int, isHeadPage: bool, macros:MasterPageMacro seq) =
            let copySystems       = Dictionary<string, string>() //copyName, orgiName
            let safeties          = HashSet<string>()
            let autoPres          = HashSet<string[]>()  //jobFqdn

            let jobInfos           = Dictionary<string, HashSet<string>>() // jobBase, api SystemNames
            let btnHeadPageDefs    = Dictionary<string, BtnType>()
            let btnDefs            = Dictionary<string, BtnType>()
            let lampHeadPageDefs   = Dictionary<string, LampType>()
            let lampDefs           = Dictionary<string, LampType>()
            let condiHeadPageDefs  = Dictionary<string, ConditionType>()
            let condiDefs          = Dictionary<string, ConditionType>()
            let actionHeadPageDefs = Dictionary<string, ActionType>()
            let actionDefs         = Dictionary<string, ActionType>()

            let mutable ifName = ""
            let mutable rootNode: bool option = None
            let mutable jobDevParam: JobDevParam = defaultJobDevParam()     // jobDevParam  param
            let mutable ifTX = ""
            let mutable ifRX = ""
            let mutable realGoingTime:CountUnitType option = None
            let mutable realRepeatCnt:CountUnitType option = None
            let mutable CallTime:CallTime option = None
            let mutable valueParamIO:ValueParamIO  = defaultValueParamIO()

          

            let updateCopySys (barckets: string, orgiSysName: string, groupJob: int) =
                if (groupJob > 0) then
                    shape.ErrorName(ErrID._19, iPage)
                else
                    let copyRows = barckets.Split(';').Select(fun s -> s.Trim())
                    let copys = copyRows.Select(fun sys -> $"{pageTitle}{TextDeviceSplit}{sys}")

                    if copys.Distinct().length() <> copys.length() then
                        Office.ErrorName(shape, ErrID._33, iPage)

                    copys
                    |> Seq.iter (fun copy ->
                        copySystems.Add(copy, orgiSysName)
                        jobInfos.Add(copy, [ copy ] |> HashSet))

            let updateDeviceIF (text: string) =
                ifName <- GetBracketsRemoveName(text) |> trimSpace |> trimNewLine

                match GetSquareBrackets(shape.InnerText, false) with
                | Some txrx ->
                    if (txrx.Contains('~')) then
                        let tx = (txrx.Split('~')[0]) |> trimSpace
                        let rx = (txrx.Split('~')[1]) |> trimSpace

                        let getRealName (apiReal: string) =
                            if apiReal = "_" || apiReal.IsEmpty then
                                failWithLog $"{ErrID._43} {shape.InnerText}"
                            apiReal

                        ifTX <- getRealName tx
                        ifRX <- getRealName rx
                    else
                        failWithLog $"{ErrID._43} {shape.InnerText}"
                | None ->
                    failWithLog $"{ErrID._53} {shape.InnerText}"

            let updateTimeNCounter() =
                let contents = GetLastBracketContents (shape.InnerText)
                realGoingTime <- parseUIntMSec contents TextTIME
                realRepeatCnt <- parseUIntMSec contents TextCOUNT
      
                
            let namePure(shape:Shape) =
                let macrosName = nameNFunc(shape, macros, iPage)
                let removeLastParentheses = GetLastParenthesesRemoveName(macrosName)
                String.Join(".", removeLastParentheses.Split('.').Select(trimSpace).Select(GetLastBracketRemoveName)) 
                |> trimSpaceNewLine

            let name =
                let nameTrim  = namePure(shape)
                GetLastParenthesesRemoveName(getTrimName(shape, nameTrim))

            //do
            let nodeType = getNodeType(shape, name, iPage)
            try
                nameCheck (shape, nodeType, iPage, name)
                match nodeType with
                |  REAL -> updateTimeNCounter()
                |  CALL -> 
                    match GetSquareBrackets(shape.InnerText, true) with
                    | Some text -> text.Split(';').Iter(fun s -> safeties.Add s |> ignore)
                    | None -> ()

                | IF_DEVICE -> updateDeviceIF shape.InnerText

                | ( OPEN_EXSYS_CALL | OPEN_EXSYS_LINK | COPY_DEV ) ->
                    let name, number = GetTailNumber(shape.InnerText)
                    match GetSquareBrackets(name, false) with
                    | Some text -> updateCopySys (text, (GetBracketsRemoveName(name) |> trimSpace), number)
                    | None -> ()
                | BUTTON ->
                    let addDic = if isHeadPage then btnHeadPageDefs else btnDefs
                    getBracketItems(shape.InnerText)
                        .ForEach(fun (n, t) -> addDic.Add(n |> TrimSpace, t |> getBtnType))
                | LAMP ->
                    let addDic = if isHeadPage then lampHeadPageDefs else lampDefs
                    getBracketItems(shape.InnerText)
                        .ForEach(fun (n, t) -> addDic.Add(n |> TrimSpace, t |> getLampType))
                | CONDITIONorAction ->
                    let addCondiDic = if isHeadPage then condiHeadPageDefs else condiDefs
                    let addActionDic = if isHeadPage then actionHeadPageDefs else actionDefs
                    getBracketItems(shape.InnerText)
                        .ForEach(fun (n, t) ->
                            match tryGetConditionType t, tryGetActionType t with
                            | Some c, _ -> addCondiDic.Add(n |> TrimSpace, c)
                            | _, Some a -> addActionDic.Add(n |> TrimSpace, a)
                            | _ -> failWithLog $"{t} is Error Type")

                | REALExF -> updateTimeNCounter()
                | (LAYOUT | AUTOPRE | DUMMY) -> ()

                let callNAutoPreName = nameNFunc(shape, macros, iPage)
                if nodeType.IsOneOf(CALL, AUTOPRE) && callNAutoPreName.Contains('.') then
                    //Dev1[3(3,3)].Api(!300, 200)[PUSH, MAX(1200ms), CHK(500ms)]
                    // names: e.g {"TT_CT"; "2ND_LATCH2[5(5,1)]"; "RET" }
                    let names = GetLastBracketRemoveName(callNAutoPreName).Split('.').ToFSharpList()
                    let devProp, apiProp =
                        match names with
                        | n1::n2::[] -> n1, n2
                        | n1::n2::n3::[] -> n2, n3
                        | _ ->
                            failwith $"Error: {callNAutoPreName}"

                    let jobPram =
                        let devP = devProp           |> trimSpace |> GetLastBracketContents    // e.g "[5(5,1)]"
                        let apiP = apiProp           |> trimSpace |> GetLastBracketRemoveName |> trimSpace |> GetLastParenthesesContents    // e.g "(!200, 300)"
                        let jobP = callNAutoPreName  |> trimSpace |> GetLastParenthesesRemoveName |> trimSpace  |>GetLastBracketContents   // e.g "[PUSH, MAX(1200ms), CHK(500ms)]]"
                        if devP = "" && apiP = "" && jobP = "" then
                            defaultJobDevParam()
                        else
                            let cntPara =    devP |> GetLastParenthesesRemoveName    // e.g "5"
                            let cntOptPara = devP |> GetLastParenthesesContents      // e.g "5,1"
                            let paraText =  // e.g "N5(5,1)"
                                match  cntPara,  cntOptPara with
                                | "", "" -> ""
                                | "", _ -> failWithLog $"err: {name}MultiAction Count >= 1 : {cntPara}"
                                | _ , "" -> $"{TextJobMulti}{cntPara}"
                                | _ -> $"{TextJobMulti}{cntPara}({cntOptPara})"
                            
                            if not (apiP.IsNullOrEmpty()) then
                                if apiP.Contains(TextInOutSplit) then
                                    let inV = createValueParam (apiP.Split(TextInOutSplit).Head().Trim() )
                                    let outV = createValueParam (apiP.Split(TextInOutSplit).Last().Trim() )
                                    valueParamIO <- ValueParamIO(inV, outV)
                                else 
                                    failWithLog $"err: {name}(input, output) {TextInOutSplit} 로 구분되어 입력해야 합니다. "

                            let jobPush = if jobP.Contains(TextCallPush) then $"{TextCallPush}" else ""
                            
                            let callT = getCallTime jobP
                            if not(callT.IsDefault)
                            then
                                CallTime <- Some callT
                            let items = [paraText; jobPush].Where(fun f->not(f.IsNullOrEmpty()))
                            getParserJobType(String.Join(";", items))


                    jobDevParam <- jobPram
            with ex ->
                shape.ErrorShape(ex.Message, iPage)


            PptNode(shape, iPage, pageTitle, slieSize, isHeadPage, macros
                    , copySystems
                    , safeties
                    , autoPres
                    , jobInfos
                    , btnHeadPageDefs
                    , btnDefs
                    , lampHeadPageDefs
                    , lampDefs
                    , condiHeadPageDefs
                    , condiDefs
                    , actionHeadPageDefs
                    , actionDefs
                    , nodeType
                    , ifName
                    , rootNode
                    //, taskDevParam
                    , jobDevParam
                    , CallTime
                    , valueParamIO
                    , ifTX
                    , ifRX
                    , realGoingTime
                    , realRepeatCnt
                    , name

            )

