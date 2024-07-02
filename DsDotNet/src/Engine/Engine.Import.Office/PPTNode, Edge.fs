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
module PPTNodeModule =

    type pptPage(slidePart: SlidePart, iPage: int, bShow: bool) =
        member x.PageNum = iPage
        member x.SlidePart = slidePart
        member x.IsUsing = bShow
        member x.Title = slidePart.PageTitle()

    type pptNode(shape: Presentation.Shape, iPage: int, pageTitle: string, slieSize: int * int, isHeadPage: bool) =
        let copySystems = Dictionary<string, string>() //copyName, orgiName
        let safeties = HashSet<string>()
        let autoPres = HashSet<string>()
        
        let jobInfos = Dictionary<string, HashSet<string>>() // jobBase, api SystemNames
        let btnHeadPageDefs = Dictionary<string, BtnType>()
        let btnDefs = Dictionary<string, BtnType>()
        let lampHeadPageDefs = Dictionary<string, LampType>()
        let lampDefs = Dictionary<string, LampType>()
        let condiHeadPageDefs = Dictionary<string, ConditionType>()
        let condiDefs = Dictionary<string, ConditionType>()

        let mutable ifName = ""
        let mutable rootNode: bool option = None
        let mutable devParam: (DevParam option * DevParam option) option = None     // Input/Output param
        let mutable ifTX = ""
        let mutable ifRX = ""
        let mutable realGoingTime:float option = None   
        let mutable realDelayTime:float option = None   

        let nodeType = getNodeType(shape, iPage)
        let name = GetLastParenthesesReplaceName( nameTrim(shape) |> getTrimName shape,  "")
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
                    let copys = copyRows.Select(fun sys -> $"{pageTitle}{TextFlowSplit}{sys}")

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

        let updateTime() = 
            let goingT, delayT = updateRealTime shape.InnerText
            realGoingTime <- goingT
            realDelayTime <- delayT

        do



            try 
                nameCheck (shape, nodeType, iPage, namePure(shape), nameNFunc(shape))
                match nodeType with
                | CALL
                | REAL ->
                    match GetSquareBrackets(shape.InnerText, true) with
                    | Some text -> updateSafety text
                    | None -> ()
                    if nodeType = REAL then updateTime()
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
                        
                | REALExF -> updateTime()
                | LAYOUT
                | AUTOPRE
                | DUMMY -> ()
            with ex ->  
                shape.ErrorShape(ex.Message, iPage)  

        member x.PageNum = iPage
        member x.Shape = shape
        member x.CopySys = copySystems
        member x.JobCallNames = jobInfos.Keys
        member x.RealFinished = shape.IsUnderlined()
        member x.RealNoTrans = if nodeType.IsReal then shape.IsStrikethrough() else failWithLog $"err: {name}RealNoTrans is not real Type"
        member x.DisableCall = if nodeType.IsCall then shape.IsStrikethrough() else failWithLog $"err: {name}CallSkipCoin is not call Type"
        member x.JobParam = 
            if (not(nodeType = CALL || nodeType = AUTOPRE) || x.IsFunction) then
                shape.ErrorName($"JobOption not support {nodeType}({name}) type", iPage)
            let flow, job, api = x.CallFlowNJobNApi
            let jobTypeAction = getJobTypeAction (api) 
            let jobTypeMulti  = getJobTypeMulti (name.Substring(0, (name.Length-api.Length-1)))
            JobParam (jobTypeAction, jobTypeMulti)
      

        member x.Safeties = safeties
        member x.AutoPres = autoPres
        member x.AutoPreCondition = x.CallName
        
        member x.RealGoingTime = realGoingTime
        member x.RealDelayTime = realDelayTime
        member x.IfName = ifName
        member x.IfTX = ifTX
        member x.IfRX = ifRX

        member x.NodeType = nodeType
        
        member x.PageTitle = pageTitle
        member x.Position = shape.GetPosition(slieSize)
        member x.OperatorName = pageTitle+TextFlowSplit+name.Replace(".", "_")
        member x.CommandName  = pageTitle+TextFlowSplit+name.Replace(".", "_")
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

        member x.UpdateTime(real: Real) =
            let checkAndUpdateTime (newTime: float option) getField setField =
                match newTime with
                | Some newValue ->
                    match getField() with
                    | Some currentValue when currentValue <> newValue ->
                        shape.ErrorName(ErrID._76, iPage)
                    | _ -> setField(Some newValue)
                | None -> ()

            checkAndUpdateTime realGoingTime (fun () -> real.DsTime.AVG) (fun v -> real.DsTime.AVG <- v)
            checkAndUpdateTime realDelayTime (fun () -> real.DsTime.TON) (fun v -> real.DsTime.TON <- v)

        member x.UpdateCallDevParm(isRoot: bool) =
            rootNode <- Some isRoot
            if nodeType = CALL then
                let isDevCall = name.Contains(".")
                let hasDevParam = GetLastParenthesesReplaceName(nameNFunc(shape), "") <> nameNFunc(shape)
                match isRoot, isDevCall with
                | true, true -> //root dev call
                    let inParam = 
                        if hasDevParam then getOperatorParam (shape, nameNFunc(shape), iPage)
                        else createDevParam "" None (Some(DuBOOL)) (Some(true)) None
                    devParam <- Some(Some(inParam), None)
                | false, true -> //real dev call
                    if hasDevParam then
                        let inParam, outParam = getCoinParam (shape, nameNFunc(shape), iPage)
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
                
            if (not(nodeType = CALL || nodeType = AUTOPRE)) then
                shape.ErrorName($"CallName not support {nodeType}({name}) type", iPage)

            let parts = GetLastParenthesesReplaceName(name, "").Split('.')  
            match parts.Length with
            | 2 -> 
                let job = $"{pageTitle}{TextFlowSplit}{TrimSpace(parts.[0])}"  |> GetBracketsRemoveName
                let api = TrimSpace(parts.[1]) |> GetBracketsRemoveName
                pageTitle, job, api
            | 3 -> 
                let job = $"{TrimSpace(parts.[0])}{TextFlowSplit}{TrimSpace(parts.[1])}"  |> GetBracketsRemoveName
                let api = TrimSpace(parts.[2]) |> GetBracketsRemoveName
                parts.[0], job, api
            | _ -> shape.ErrorShape("Action이름 규격을 확인하세요.", iPage)  


        member x.CallName = 
        
            let flow, job, api = x.CallFlowNJobNApi
            $"{job}_{api}"


        member x.CallDevName = 
            let flow, job, api = x.CallFlowNJobNApi
            job    
            
        member x.FlowName = 
            let flow, job, api = x.CallFlowNJobNApi
            flow

        

        member x.IsAlias: bool = x.Alias.IsSome
        member val Alias: pptNode option = None with get, set
        member val Id = shape.GetId()
        member val Key = Objkey(iPage, shape.GetId())
        member val Name = name with get, set
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

