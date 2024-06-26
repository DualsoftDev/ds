namespace Engine.Core

open System.Linq
open Dual.Common.Core.FS
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Reflection     
open System

[<AutoOpen>]
module internal ToDsTextModule =
    let getTab n = Seq.init n (fun _ -> "    ") |> String.concat ""
    let lb, rb = "{", "}"
    let lbCode, rbCode = "#{", "}"
    let combineLines = ofNotNullAny >> joinLines
    let mutable  pCooment = true //printComment

    let getName (v:Vertex) =
            match v with    
            | :? Call as c when c.IsPureCommand -> (getRawName v.PureNames true) + "()"
            | :? Call as c when c.IsPureOperator -> "#"+(getRawName v.PureNames true)
                
            |_-> getRawName v.PureNames true    

    type private MEI = ModelingEdgeInfo<Vertex>
    let private modelingEdgeInfosToDs (es:MEI seq) (tab:string) =

        (* rss : Result edge Set of Set *)
        let folder (rss:ResizeArray<MEI> list) (e:MEI) : ResizeArray<MEI> list =
            if rss.IsEmpty then
                [[e] |> ResizeArray]
            else
                match rss.TryFind(fun rs -> rs[0].Sources = e.Targets) with
                | Some rs -> rs.Insert(0, e); rss
                | _ ->
                    match rss.TryFind(fun rs -> rs.Last().Targets = e.Sources) with
                    | Some rs -> rs.Add(e); rss
                    | _ ->
                        ([e] |> ResizeArray)::rss


        let es  = es |> Seq.sortBy(fun e -> e.EdgeSymbol.Count(fun ch -> ch = '|'))
        let ess = es |> Seq.fold folder []

            
        let getNames (vs:Vertex seq) = vs.Select(getName).JoinWith(", ")
        [
            for es in ess do
                let comments = ResizeArray<string>()
                yield [
                    yield $"{tab}"
                    for i, e in es.Indexed() do
                        let ss, ts = e.Sources, e.Targets
                        let mutable ssn = getNames ss
                        let tn = getNames ts
                        if i <> 0 then ssn <- ""
                        let arrow = e.EdgeSymbol
                        yield $"{ssn} {arrow} {tn}"
                        let comment =
                            let getVsAndTypes (vs:Vertex seq) =
                                [ for v in vs -> $"{getName v}({v.GetType().Name})" ].JoinWith(", ")
                            let sn2 = if ssn <> "" then $"{getVsAndTypes ss}" else " "
                            $"{sn2}{arrow} {getVsAndTypes ts}"
                        comments.Add($"{comment}")
                    yield ";"
                    if pCooment then yield ("\t\t// " + comments.JoinWith("") + ";")
                ] |> String.concat ""
        ]

    let rec graphToDs (container:ParentWrapper) (indent:int) =
        let tab = getTab indent
        let graph = container.GetGraph()
        [
            yield! modelingEdgeInfosToDs (container.GetModelingEdges()) tab

            let stems = graph.Vertices.OfType<Real>().Where(fun r -> r.Graph.Vertices.Any()).ToArray()
            for stem in stems do
                yield $"{tab}{stem.Name.QuoteOnDemand()} = {lb}"
                yield! graphToDs (DuParentReal stem) (indent + 1)
                yield $"{tab}{rb}"

            let notMentioned = graph.Islands.Except(stems.Cast<Vertex>()).ToArray()
            if notMentioned.any() 
            then 
                let comment  = if pCooment then "// island"  else ""
                let isLandCommas =  notMentioned.Select(fun i -> getName i).JoinWith(", ")
                yield $"{getTab (indent)}{isLandCommas}; {comment}"
        ]

    let flowToDs (flow:Flow) (indent:int) =
        let getName (xs:string array) = getRawName xs true
        let tab = getTab indent
        [
            yield $"{tab}[flow] {flow.Name.QuoteOnDemand()} = {lb}"
            yield! graphToDs (DuParentFlow flow) (indent+1)
            let aliasDefExist = flow.AliasDefs.Values  //외부 Flow Alias 외에 하나라도 있으면
                                    .Where(fun a-> a.AliasTexts.Count <> a.AliasTexts.Where(fun w->w.Contains('.')).length())
                                    .any()     
                                
            if aliasDefExist then
                let tab = getTab (indent+1)
                yield $"{tab}[aliases] = {lb}"
                for a in flow.AliasDefs.Values do
                    let toTextAlias = a.AliasTexts.Where(fun f->not(f.Contains('.'))).ToArray()
                    if toTextAlias.any()
                    then
                        let aliasTexts = (toTextAlias.Select(fun f->f.QuoteOnDemand()) |> String.concat "; ") + ";"
                        let tab = getTab (indent+2)
                        let aliasKey =
                            match a.AliasTarget with
                            | Some(DuAliasTargetReal real) -> real.GetAliasTargetToDs(flow) |> getName
                            | Some(DuAliasTargetCall call) -> call.GetAliasTargetToDs() |> getName
                            | None -> failwithlog "ERROR"

                        yield $"{tab}{aliasKey} = {lb} {aliasTexts} {rb}"
                yield $"{tab}{rb}"

            yield $"{tab}{rb}"
        ] |> combineLines

    let printVariables (theSystem:DsSystem) =
        let vars = theSystem.Variables
        let tab = getTab 1
        let tab2 = getTab 2
        [
            if vars.Any() then
                yield $"{tab}[variables] = {lb}"
                for var in vars do
                    yield $"{tab2}{var.ToDsText()};"
                yield $"{tab}{rb}"
            elif theSystem.Functions.Any() then
                yield $"{tab}[variables] = {lb}{rb}"
        ] |> combineLines

    let getInOutTypeText name (inDataType:DataType) (outDataType:DataType) =
        match inDataType, outDataType with
        | DuBOOL, DuBOOL -> $"{name}"
        | i, o when i = o -> $"{name}(type:{inDataType.ToText()})"
        | _ ->  $"{name}(type:{inDataType.ToText()}, {outDataType.ToText()})"

    let rec systemToDs (system:DsSystem) (indent:int) (printComment:bool)=
        pCooment <- printComment
        let tab = getTab indent
        let tab2 = getTab 2
        let tab3 = getTab 3
       
        [
            yield $"[sys] {system.Name.QuoteOnDemand()} = {lb}"

            for f in system.Flows do
                yield flowToDs f indent

            if system.Jobs.Any() then
                let printDev (ai:TaskDev) jobName= $"{ai.ApiName}({toTextDevParam (ai.GetInParam(jobName))}, {toTextDevParam (ai.GetOutParam(jobName))})"
                yield $"{tab}[jobs] = {lb}"
                for c in system.Jobs do
                    let jobItems =
                        c.DeviceDefs
                        |> Seq.map (fun d-> printDev d (c.Name))
                          
                    let jobItemText = jobItems.JoinWith("; ") + ";"
                    if c.JobParam.ToText() = ""
                    then
                        yield $"{tab2}{c.Name.QuoteOnDemand()} = {lb} {jobItemText} {rb}"  
                    else 
                        yield $"{tab2}{c.Name.QuoteOnDemand()}[{c.JobParam.ToText()}] = {lb} {jobItemText} {rb}"  

                yield $"{tab}{rb}"
            elif system.Functions.Any() then
                yield $"{tab}[jobs] = {lb}{rb}" 
            yield printVariables system

            let funcCodePrint funcName (code:string) =
                let codeLines = code.Split([|"\r\n"; "\n"|], StringSplitOptions.None)
                [
                    if codeLines.length() > 1
                    then 
                        yield $"{tab2}{funcName} = {lbCode}"
                        for line in codeLines  
                            do yield $"{tab3}{line}"
                        yield $"{tab2}{rbCode}"
                    else 
                        yield $"{tab2}{funcName} = {lbCode}{code}{rbCode}"
                ]

            let operators = system.Functions.OfType<OperatorFunction>()
            if operators.Any() then
                yield $"{tab}[operators] = {lb}"
                for op in operators do
                    let name = op.Name.QuoteOnDemand()
                    if op.OperatorType = DuOPUnDefined
                    then 
                        yield $"{tab2}{name};"
                    else 
                        yield! funcCodePrint name (op.ToDsText())

                yield $"{tab}{rb}"

            let commands = system.Functions.OfType<CommandFunction>()
            if commands.Any() then
                yield $"{tab}[commands] = {lb}"
                for cmd in commands do
                    let name = cmd.Name.QuoteOnDemand()
                    if cmd.CommandType = DuCMDUnDefined ||  cmd.CommandCode = ""
                    then 
                        yield $"{tab2}{name};"
                    else
                        yield! funcCodePrint $"{name}" (cmd.ToDsText())

                yield $"{tab}{rb}"

            if system.ApiItems.Any() then
                yield $"{tab}[interfaces] = {lb}"
                for item in system.ApiItems do
                    let ser =
                        let getFlowAndRealName (r:Real) = [r.Flow.Name; r.Name].Combine()
                        let coverWithUnderScore (x:string) = if x.IsNullOrEmpty() then "_" else x
                        let s = getFlowAndRealName(item.TX) |> coverWithUnderScore
                        let e = getFlowAndRealName(item.RX) |> coverWithUnderScore
                        $"{s} ~ {e}"
                    yield $"{tab2}{item.Name.QuoteOnDemand()} = {lb} {ser} {rb}"

                for ri in system.ApiResetInfos.Where(fun a->not(a.AutoGenByFlow)) do
                    yield $"{tab2}{ri.ToDsText()};"

                yield $"{tab}{rb}"

            let btns =
                let allBtns = [
                    system.AutoHWButtons;
                    system.ManualHWButtons;
                    system.DriveHWButtons;
                    system.PauseHWButtons;
                    system.ClearHWButtons;
                    system.EmergencyHWButtons;
                    system.TestHWButtons;
                    system.HomeHWButtons;
                ]
                allBtns
                |> List.map(fun b -> b |> List.ofSeq)
                |> List.collect id


            let HwSystemToDs(category:string, hws:HwSystemDef seq) =
                let getHwInfo(hw:HwSystemDef) = 
                    let flows = hw.SettingFlows.Select(fun f -> f.NameComponents.Skip(1).Combine().QuoteOnDemand())
                    let itemText = if flows.any() then (flows |> String.concat "; ") + ";" else ""
                    let inAddr =  addressPrint  hw.InAddress  
                    let outAddr = addressPrint  hw.OutAddress 
                    itemText, inAddr, outAddr

                [
                    match hws.length() with
                    |0-> ()
                    |1-> 
                        let itemText, inAddr, outAddr = getHwInfo (hws.Head())
                        yield $"{tab2}[{category}] = {lb} {hws.Head().Name.QuoteOnDemand()}({inAddr}, {outAddr}) = {lb} {itemText} {rb} {rb}"
                    |_->
                        yield $"{tab2}[{category}] = {lb}"
                        for hw in hws do
                            let itemText, inAddr, outAddr = getHwInfo hw

                            yield $"{tab3}{getInOutTypeText (hw.Name.QuoteOnDemand()) hw.InParam.Type hw.OutParam.Type }({inAddr}, {outAddr}) = {lb} {itemText} {rb}"
                          
                        yield $"{tab2}{rb}"
                ] |> combineLines


            if btns.Any() then
                yield $"{tab}[buttons] = {lb}"                
                yield HwSystemToDs("a", system.AutoHWButtons.Cast<HwSystemDef>())
                yield HwSystemToDs("m", system.ManualHWButtons.Cast<HwSystemDef>())
                yield HwSystemToDs("d", system.DriveHWButtons.Cast<HwSystemDef>())
                yield HwSystemToDs("e", system.EmergencyHWButtons.Cast<HwSystemDef>())
                yield HwSystemToDs("t", system.TestHWButtons.Cast<HwSystemDef>())
                yield HwSystemToDs("r", system.ReadyHWButtons.Cast<HwSystemDef>())
                yield HwSystemToDs("p", system.PauseHWButtons.Cast<HwSystemDef>())
                yield HwSystemToDs("c", system.ClearHWButtons.Cast<HwSystemDef>())
                yield HwSystemToDs("h", system.HomeHWButtons.Cast<HwSystemDef>())
                yield $"{tab}{rb}"


            if system.HWLamps.Any() then
                yield $"{tab}[lamps] = {lb}"
                yield HwSystemToDs("a", system.AutoHWLamps.Cast<HwSystemDef>())
                yield HwSystemToDs("m", system.ManualHWLamps.Cast<HwSystemDef>())
                yield HwSystemToDs("d", system.DriveHWLamps.Cast<HwSystemDef>())
                yield HwSystemToDs("e", system.ErrorHWLamps.Cast<HwSystemDef>())
                yield HwSystemToDs("t", system.TestHWLamps.Cast<HwSystemDef>())
                yield HwSystemToDs("r", system.ReadyHWLamps.Cast<HwSystemDef>())
                yield HwSystemToDs("i", system.IdleHWLamps.Cast<HwSystemDef>())
                yield HwSystemToDs("o", system.OriginHWLamps.Cast<HwSystemDef>())

                yield $"{tab}{rb}"

            let cnds = system.HWConditions
            if cnds.Any() then
                let getTargetCnds (target:ConditionType) =
                    cnds |> Seq.filter(fun c -> c.ConditionType = target)
                let driveCnds = getTargetCnds DuDriveState
                let readyCnds = getTargetCnds DuReadyState
                yield $"{tab}[conditions] = {lb}"
                yield HwSystemToDs("d", driveCnds.Cast<HwSystemDef>())
                yield HwSystemToDs("r", readyCnds.Cast<HwSystemDef>())
                yield $"{tab}{rb}"

            (* prop
                    safety
                    layouts *)
            let safetyHolders =
                [   for f in system.Flows do
                        yield! f.Graph.Vertices.OfType<ISafetyConditoinHolder>()

                        for r in f.Graph.Vertices.OfType<Real>() do
                        yield! r.Graph.Vertices.OfType<ISafetyConditoinHolder>()
                ] |> List.distinct

            let getCallName (call:Call) =
                    match call.Parent with
                    | DuParentReal r-> $"{r.Flow.Name.QuoteOnDemand()}.{call.ParentNPureNames.Combine()}"
                    | DuParentFlow _ -> call.ParentNPureNames.Combine()

            let withSafeties = safetyHolders.Where(fun h -> h.SafetyConditions.Any())
            let safeties =
                
                let safetyConditionName (sc:SafetyCondition) =
                    match sc with
                    | DuSafetyConditionReal real       -> real.ParentNPureNames.Combine()
                    | DuSafetyConditionCall call       -> (getCallName call)
                let safetyConditionHolderName(sch:ISafetyConditoinHolder) =
                    match sch with
                    | :? Real as real -> real.ParentNPureNames.Combine()
                    | :? Call as call -> (getCallName call)
                    | _ -> failwithlog "ERROR"

                [
                    if withSafeties.Any() then
                        yield $"{tab2}[safety] = {lb}"
                        for safetyHolder in withSafeties do
                            let conds = safetyHolder.SafetyConditions.Select(safetyConditionName).JoinWith("; ") + ";"
                            yield $"{tab3}{safetyConditionHolderName safetyHolder} = {lb} {conds} {rb}"
                        yield $"{tab2}{rb}"
                ] |> combineLines


                
            let autoPreHolders =
                [   for f in system.Flows do
                        yield! f.Graph.Vertices.OfType<IAutoPrerequisiteHolder>()

                        for r in f.Graph.Vertices.OfType<Real>() do
                        yield! r.Graph.Vertices.OfType<IAutoPrerequisiteHolder>()
                ] |> List.distinct

            let withAutoPres = autoPreHolders.Where(fun h -> h.AutoPreConditions.Any())
            let autoPres =
                [
                    if withAutoPres.Any() then
                        yield $"{tab2}[autopre] = {lb}"
                        for autoPreHolder in withAutoPres do
                            let conds = autoPreHolder.AutoPreConditions.Select(fun v->v.GetAutoPreCall() |> getCallName).JoinWith("; ") + ";"
                            yield $"{tab3}{autoPreHolder.GetAutoPreCall()|>getCallName} = {lb} {conds} {rb}"
                        yield $"{tab2}{rb}"
                ] |> combineLines
            
            
            
            let layoutList = system.LoadedSystems 
                             |> Seq.collect (fun f-> f.ChannelPoints.Select(fun kv->kv.Key))
                             |> Seq.distinct

            let layoutPointDic = layoutList
                                 |> Seq.map (fun f-> f, system.LoadedSystems 
                                                        |> Seq.collect (fun dev-> dev.ChannelPoints.Select(fun kv-> dev.Name.QuoteOnDemand(), kv.Key, kv.Value))
                                                        |> Seq.filter (fun (_, ch, _)-> ch = f)
                                                        )
                                 |> dict

            let layouts =
                let makeList (name:string) (xywh:Xywh) =
                    let posi =
                        if xywh.W.HasValue then
                            $"({xywh.X}, {xywh.Y}, {xywh.W.Value}, {xywh.H.Value});"
                        else
                            $"({xywh.X}, {xywh.Y});"
                    $"{tab3}{name} = {posi}"
                [
                    for file in layoutList do
                        if file = TextEmtpyChannel 
                        then
                            yield $"{tab2}[layouts] = {lb}"
                        else 
                            yield $"{tab2}[layouts file={quote file}] = {lb}"

                        for device, _, xy in layoutPointDic[file] do
                            yield makeList device xy

                        yield $"{tab2}{rb}"                  
                ] |> combineLines


            let reals = system.GetRealVertices()
            let finishedReals = reals.Filter(fun f->f.Finished)
            let noTransDataReals =  reals.Filter(fun f->f.NoTransData)
            let path3DReals = reals.Where(fun f->f.Path3D.IsSome)
            let scriptReals = reals.Where(fun f->f.Script.IsSome)

            let path3Ds = 
                [
                    if path3DReals.Any() then
                        yield $"{tab2}[actions] = {lb}"
                        for real in path3DReals do
                            yield $"{tab3}{real.Flow.Name.QuoteOnDemand()}.{real.Name.QuoteOnDemand()} = {lb}{real.Path3D.Value}{rb};"
                        yield $"{tab2}{rb}"
                ] |> combineLines

            let scripts = 
                [
                    if scriptReals.Any() then
                        yield $"{tab2}[scripts] = {lb}"
                        for real in scriptReals do
                            yield $"{tab3}{real.Flow.Name.QuoteOnDemand()}.{real.Name.QuoteOnDemand()} = {lb}{real.Script.Value}{rb};"
                        yield $"{tab2}{rb}"
                ] |> combineLines

            let finished = 
                [
                    if finishedReals.Any() then
                        yield $"{tab2}[finish] = {lb}"
                        for real in finishedReals do
                            yield $"{tab3}{real.Flow.Name.QuoteOnDemand()}.{real.Name.QuoteOnDemand()};"
                        yield $"{tab2}{rb}"
                ] |> combineLines

            let noTransData = 
                [
                    if noTransDataReals.Any() then
                        yield $"{tab2}[notrans] = {lb}"
                        for real in noTransDataReals do
                            yield $"{tab3}{real.Flow.Name.QuoteOnDemand()}.{real.Name.QuoteOnDemand()};"
                        yield $"{tab2}{rb}"
                ] |> combineLines

            let disabledVertices = 
                [
                    for flow in system.Flows do
                    for rootVert in flow.Graph.Vertices do
                        match rootVert with
                        | :? Real as real ->
                            for vert in real.Graph.Vertices do
                                match vert with
                                | :? Call as call ->
                                    if call.Disabled then yield call
                                | _ -> ()
                        | _ -> ()
                ]
            let disabled = 
                [
                    if disabledVertices.Any() then
                        yield $"{tab2}[disable] = {lb}"
                        for vert in disabledVertices do
                            let compo = vert.NameComponents
                            yield $"{tab3}{compo[1].QuoteOnDemand()}.{compo[2].QuoteOnDemand()}.{compo[3].QuoteOnDemand()};"
                        yield $"{tab2}{rb}"
                ] |> combineLines

            if safeties.Any() || autoPres.Any() || layouts.Any() || finished.Any() || disabled.Any()|| noTransData.Any() then
                yield $"{tab}[prop] = {lb}"
                if safeties.Any()  then yield safeties
                if autoPres.Any()  then yield autoPres
                if layouts.Any()   then yield layouts

                if path3Ds.Any()  then yield path3Ds
                if scripts.Any()  then yield scripts
                if finished.Any()  then yield finished
                if disabled.Any()  then yield disabled
                if noTransData.Any()  then yield noTransData
                
                yield $"{tab}{rb}"
            let commentDevice(absoluteFilePath:string) = if pCooment then  $"// {absoluteFilePath}" else "";
         
            let groupedDevices = 
                system.Devices
                |> Seq.groupBy (fun d -> d.RelativeFilePath)  

            for (path, devices) in groupedDevices do
                
                let textDevices = 
                    if devices.Count() = 1 then
                        devices.First().Name.QuoteOnDemand()
                    else
                        $"\r\n{tab2}" + String.Join($",\r\n{tab2}", devices.Select(fun d -> d.Name.QuoteOnDemand()))
                    
                yield $"{tab}[device file={quote path}] {textDevices}; {commentDevice (devices.First().AbsoluteFilePath)}"

            for es in system.ExternalSystems do
                yield $"{tab}[external file={quote es.RelativeFilePath}] {es.Name}; {commentDevice es.AbsoluteFilePath}"
            

            //Commands/Observes는 JobDef에 저장 (Variables는 OriginalCodeBlocks ?? System.Variables ??)
            //yield codeBlockToDs system

            // todo 복수개의 block 이 허용되면, serialize 할 때 해당 위치에 맞춰서 serialize 해야 하는데...
            //for code in system.OriginalCodeBlocks do
            //    yield code
            
            // code 는 [Commands] = { cmd1 = ${code1}$, cmd2 = ${code2}$ } 복수개 저장

            yield rb
            yield $"//DS Language Version = [{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion}]"
            yield $"//DS Library Date = [{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyDescriptionAttribute>().Description}]"
            yield $"//DS Engine Version = [{Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyFileVersionAttribute>().Version}]"

        ] |> combineLines

    type DsSystem with
        member x.ToDsText(printComment:bool) = systemToDs x 1 printComment


[<Extension>]
type SystemToDsExt =
    [<Extension>] static member ToDsText (system:DsSystem, printComment:bool) = systemToDs system 1 printComment
