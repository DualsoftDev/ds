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
module PPTNodeModule =

    type pptPage(slidePart: SlidePart, iPage: int, bShow: bool) =
        member x.PageNum = iPage
        member x.SlidePart = slidePart
        member x.IsUsing = bShow
        member x.Title = slidePart.PageTitle()

    type pptNode(shape: Presentation.Shape, iPage: int, pageTitle: string, slieSize: int * int, isHeadPage: bool) =
        let copySystems = Dictionary<string, string>() //copyName, orgiName
        let safeties = HashSet<string>()
        let jobInfos = Dictionary<string, HashSet<string>>() // jobBase, api SystemNames
        let btnHeadPageDefs = Dictionary<string, BtnType>()
        let btnDefs = Dictionary<string, BtnType>()
        let lampHeadPageDefs = Dictionary<string, LampType>()
        let lampDefs = Dictionary<string, LampType>()
        let condiHeadPageDefs = Dictionary<string, ConditionType>()
        let condiDefs = Dictionary<string, ConditionType>()

        let mutable ifName = ""
        let mutable rootNode: bool option = None
        let mutable devParam: (DevParam option * DevParam option) option = None
        let mutable ifTX = ""
        let mutable ifRX = ""

        let trimNewLine (text: string) = text.Replace("\n", "")
        let trimSpace (text: string) = text.TrimStart(' ').TrimEnd(' ')
        let trimSpaceNewLine (text: string) = text |> trimSpace |> trimNewLine
        let trimStartEndSeq (texts: string seq) = texts |> Seq.map trimSpace
        let nameNFunc = shape.InnerText.Replace("”", "\"").Replace("“", "\"") |> GetHeadBracketRemoveName |> trimSpaceNewLine //ppt “ ” 입력 호환
        let namePure = GetLastParenthesesReplaceName(nameNFunc, "") |> trimSpaceNewLine
        let nameTrim = String.Join('.', namePure.Split('.').Select(trimSpace)) |> trimSpaceNewLine

        let getPostParam(x:DevParam) =
            match x.DevValue, x.DevTime with 
            | Some v, None -> $"{v}"
            | Some v, Some t -> $"{v}_{t}ms"
            | None, Some t -> $"{t}ms"
            | None, None -> $""


        let getOperatorParam (name:string) = 
            try
                let func = GetLastParenthesesContents(name) |> trimSpaceNewLine
                if func.Contains(",") then 
                    failwithf $"{name} 입출력 규격을 확인하세요. \r\nDevice.Api(입력) 규격 입니다"
                $"{'-'}:{func}" |> getDevParam
            with _ ->
                shape.ErrorName(ErrID._70, iPage)

        let getCoinParam (name:string) = 
            let error()  = $"{name} 입출력 규격을 확인하세요. \r\nDevice.Api(입력, 출력) 규격 입니다. \r\n기본예시(300,500) 입력생략(-,500) 출력생략(300, -)"
            try
                let func = GetLastParenthesesContents(name) |> trimSpaceNewLine
                if not(func.Contains(",")) then 
                    failwithlog (error())

                let inFunc, outFunc =
                    func.Split(",").Head() |> trimSpaceNewLine,
                    func.Split(",").Last() |> trimSpaceNewLine

                let getParam x = 
                    if x = TextSkip then 
                        "" |> getDevParam
                    else 
                        $":{x}" |> getDevParam

                getParam inFunc, getParam outFunc
            with _ ->
                shape.ErrorName((error()), iPage)

        let getTrimName(nameTrim:string) =
            match GetSquareBrackets(nameTrim, false) with
            | Some text -> 
                let pureName = nameTrim |> GetBracketsRemoveName |> trimSpaceNewLine
                if shape.IsHomePlate() then pureName //AA [xxx ~ yyy] 
                else $"{pureName}[{text}]"   //AA[4] 
            | None -> nameTrim

        let getNodeType() =
            let nameNfunc = GetBracketsRemoveName(shape.InnerText)
            let name = GetLastParenthesesReplaceName(nameNfunc, "")

            match shape with
            | s when s.IsRectangle() ->
                if name.Contains(".") then REALExF else REAL
            | s when s.IsHomePlate() -> IF_DEVICE
            | s when s.IsFoldedCornerPlate() -> OPEN_EXSYS_CALL
            | s when s.IsFoldedCornerRound() -> COPY_DEV
            | s when s.IsEllipse() -> CALL
            | s when s.IsBevelShapePlate() -> LAMP
            | s when s.IsBevelShapeRound() -> BUTTON
            | s when s.IsBevelShapeMaxRound() -> CONDITION
            | s when s.IsLayout() -> shape.ErrorName(ErrID._62, iPage)
            | _ ->
                failWithLog ErrID._1

        let nodeType = getNodeType()
        let disableCall = shape.IsDashShape()
        let name = getTrimName nameTrim

        do
            let updateSafety (barckets: string) =
                barckets.Split(';')
                    |> Seq.iter (fun f ->
                        if f.Split('.').Length > 1 then
                            safeties.Add(f) |> ignore
                        else
                            failwithf $"{ErrID._74}"
                    )

            let updateCopySys (barckets: string, orgiSysName: string, groupJob: int) =
                if (groupJob > 0) then
                    shape.ErrorName(ErrID._19, iPage)
                else
                    let copyRows = barckets.Split(';').Select(fun s -> s.Trim())
                    let copys = copyRows.Select(fun sys -> $"{pageTitle}_{sys}")

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
                            if apiReal = "_" || apiReal.IsEmpty() then
                                failWithLog ErrID._43
                            apiReal

                        ifTX <- getRealName tx
                        ifRX <- getRealName rx
                    else
                        failWithLog ErrID._43
                | None ->
                    failWithLog ErrID._53

            let getBracketItems (name: string) =
                name.Split('[').Select(fun w -> w.Trim()).Where(fun w -> w <> "")
                |> Seq.map (fun f ->
                    match GetSquareBrackets("[" + f, true) with
                    | Some item -> GetBracketsRemoveName("[" + f.TrimEnd('\n')), item
                    | None -> GetBracketsRemoveName("[" + f.TrimEnd('\n')), "")


            try 
                nameCheck (shape, nodeType, iPage, namePure, nameNFunc)
                match nodeType with
                | CALL
                | REAL ->
                    match GetSquareBrackets(shape.InnerText, true) with
                    | Some text -> updateSafety text
                    | None -> ()
                | IF_DEVICE -> updateDeviceIF shape.InnerText
                | OPEN_EXSYS_CALL
                | OPEN_EXSYS_LINK
                | COPY_DEV ->
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
                | CONDITION ->
                    let addDic = if isHeadPage then condiHeadPageDefs else condiDefs
                    getBracketItems(shape.InnerText)
                        .ForEach(fun (n, t) -> addDic.Add(n |> TrimSpace, t |> getConditionType))
                | REALExF
                | LAYOUT
                | DUMMY -> ()
            with ex ->  
                shape.ErrorShape(ex.Message, iPage)  

        member x.PageNum = iPage
        member x.Shape = shape
        member x.CopySys = copySystems
        member x.JobCallNames = jobInfos.Keys
        member x.RealFinished = shape.IsUnderlined()
        member x.RealNoTrans = shape.IsStrikethrough()
        member x.Safeties = safeties

        member x.IfName = ifName
        member x.IfTX = ifTX
        member x.IfRX = ifRX

        member x.NodeType = nodeType
        member x.DisableCall = disableCall
        member x.PageTitle = pageTitle
        member x.Position = shape.GetPosition(slieSize)
        member x.OperatorName = pageTitle+"__"+name.Replace(".", "_")
        member x.CommandName  = pageTitle+"__"+name.Replace(".", "_")
        member x.IsCall = nodeType = CALL
        member x.IsCallDevParam = nodeType = CALL && devParam.IsSome 
        member x.IsRootNode = rootNode
        member x.IsFunction = x.IsCall && not(name.Contains("."))
        member x.DevParam = devParam
        member x.DevParamIn = 
            if devParam.IsSome && (devParam.Value |> fst).IsSome then 
                (devParam.Value |> fst).Value 
            else "" |> defaultDevParam     
        member x.DevParamOut = 
            if devParam.IsSome && (devParam.Value |> snd).IsSome then 
                (devParam.Value |> snd).Value 
            else "" |> defaultDevParam     

        member x.JobName =
            let flow, job, Api = x.CallFlowNJobNApi
            let pureJob = $"{job}_{Api}"
            let jobName =
                if x.IsCallDevParam then
                    let inParam = devParam.Value |> fst 
                    let outParam = devParam.Value |> snd
                    if inParam.IsSome && outParam.IsNone then
                        let post = getPostParam inParam.Value
                        if post = "" then $"{pureJob}" else $"{pureJob}_IN{post}"
                    elif inParam.IsSome && outParam.IsSome then
                        let postIn = getPostParam inParam.Value
                        let postOut = getPostParam outParam.Value
                        if postIn = "" && postOut = "" then $"{pureJob}"
                        else $"{pureJob}_IN{postIn}_OUT{postOut}" 
                    else failwithlog "error"
                else pureJob
            jobName

        member x.UpdateCallDevParm(isRoot: bool) =
            rootNode <- Some isRoot
            if nodeType = CALL then
                let isDevCall = name.Contains(".")
                let hasDevParam = GetLastParenthesesReplaceName(nameNFunc, "") <> nameNFunc
                match isRoot, isDevCall with
                | true, true -> //root dev call
                    let inParam = 
                        if hasDevParam then getOperatorParam nameNFunc
                        else createDevParam "" None (Some(DuBOOL)) (Some(true)) None
                    devParam <- Some(Some(inParam), None)
                | false, true -> //real dev call
                    if hasDevParam then
                        let inParam, outParam = getCoinParam nameNFunc
                        devParam <- Some(Some(inParam), Some(outParam))
                | _ ->  
                    if hasDevParam then failWithLog "function call 'devParam' not support"

        member x.UpdateCallProperty(call: Call) =
            call.Disabled <- x.DisableCall
            if x.IsCallDevParam && x.IsRootNode.Value = false then 
                call.TargetJob.DeviceDefs.Iter(fun d->
                    d.AddOrUpdateInParam(x.JobName , (x.DevParam.Value |> fst).Value)
                    d.AddOrUpdateOutParam(x.JobName , (x.DevParam.Value |> snd).Value)
                )
      
        member x.CallFlowNJobNApi = 
                
            if (nodeType <> CALL) then
                shape.ErrorName($"CallName not support {nodeType}({name}) type", iPage)

            let parts = GetLastParenthesesReplaceName(name, "").Split('.')  
            match parts.Length with
            | 2 -> 
                let job = $"{pageTitle}__{TrimSpace(parts.[0])}"  |> GetBracketsRemoveName
                let api = TrimSpace(parts.[1]) |> GetBracketsRemoveName
                pageTitle, job, api
            | 3 -> 
                let job = $"{TrimSpace(parts.[0])}__{TrimSpace(parts.[1])}"  |> GetBracketsRemoveName
                let api = TrimSpace(parts.[2]) |> GetBracketsRemoveName
                parts.[0], job, api
            | _ -> failwith "Invalid format in name"


        member x.CallName = 
        
            let flow, job, api = x.CallFlowNJobNApi
            $"{job}_{api}"


        member x.CallDevName = 
            let flow, job, api = x.CallFlowNJobNApi
            job    
            
        member x.FlowName = 
            let flow, job, api = x.CallFlowNJobNApi
            flow

        member x.JobParam = 
            if (nodeType <> CALL || x.IsFunction) then
                shape.ErrorName($"JobOption not support {nodeType}({name}) type", iPage)
            let flow, job, api = x.CallFlowNJobNApi
            let jobTypeAction = getJobTypeAction (api) 
            let jobTypeMulti  = getJobTypeMulti (name.Substring(0, (name.Length-api.Length-1)))
            
            { JobAction = jobTypeAction; JobMulti = jobTypeMulti}
      
        member val Id = shape.GetId()
        member val Key = Objkey(iPage, shape.GetId())
        member val Name = name with get, set
        member x.IsAlias: bool = x.Alias.IsSome
        member val Alias: pptNode option = None with get, set
        member val AliasNumber: int = 0 with get, set
        member val ButtonHeadPageDefs = btnHeadPageDefs
        member val ButtonDefs = btnDefs
        member val LampHeadPageDefs = lampHeadPageDefs
        member val LampDefs = lampDefs
        member val CondiHeadPageDefs = condiHeadPageDefs
        member val CondiDefs = condiDefs
        member x.GetRectangle(slideSize: int * int) = shape.GetPosition(slideSize)

    and pptEdge(conn: Presentation.ConnectionShape, iEdge: UInt32Value, iPage: int, sNode: pptNode, eNode: pptNode) =

        let (causal:ModelingEdgeType), (reverse:bool) = GetCausal(conn, iPage, sNode.Name, eNode.Name)

        member x.PageNum = iPage
        member x.ConnectionShape = conn
        member x.Id = iEdge
        member x.IsInterfaceEdge: bool = x.StartNode.NodeType.IsIF || x.EndNode.NodeType.IsIF
        member x.StartNode: pptNode = if (reverse) then eNode else sNode
        member x.EndNode: pptNode = if (reverse) then sNode else eNode
        member x.ParentId = 0 //reserve

        member val Name = conn.EdgeName()
        member val Key = Objkey(iPage, iEdge)

        member x.Text =
            let sName =
                match sNode.Alias with
                | Some a -> a.Name
                | None -> sNode.Name

            let eName =
                match eNode.Alias with
                | Some a -> a.Name
                | None -> eNode.Name

            if (reverse) then
                $"{iPage};{eName}{causal.ToText()}{sName}"
            else
                $"{iPage};{sName}{causal.ToText()}{eName}"

        member val Causal: ModelingEdgeType = causal

