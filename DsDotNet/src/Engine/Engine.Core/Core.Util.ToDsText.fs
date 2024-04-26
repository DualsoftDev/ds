namespace Engine.Core

open System.Linq
open Dual.Common.Core.FS
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Reflection     

[<AutoOpen>]
module internal ToDsTextModule =
    let getTab n = Seq.init n (fun _ -> "    ") |> String.concat ""
    let lb, rb = "{", "}"
    let combineLines = ofNotNullAny >> joinLines
    let mutable  pCooment = true //printComment

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
        let getName (v:Vertex) =
            match v with    
            | :? Call as c when c.TargetHasFuncOnly -> "$"+(getRawName v.PureNames true)
            |_-> getRawName v.PureNames true    
            
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
                let isLandCommas =  notMentioned.Select(fun i -> i.Name.QuoteOnDemand()).JoinWith(", ")
                yield $"{getTab (indent)}{isLandCommas}; {comment}"
        ]

    let flowToDs (flow:Flow) (indent:int) =
        let getName (xs:string array) = getRawName xs true
        let tab = getTab indent
        [
            yield $"{tab}[flow] {flow.Name.QuoteOnDemand()} = {lb}"
            yield! graphToDs (DuParentFlow flow) (indent+1)

            let aliasDefs = flow.AliasDefs.Values
            if aliasDefs.Any() then
                let tab = getTab (indent+1)
                yield $"{tab}[aliases] = {lb}"
                for a in aliasDefs do
                    let mnemonics = (a.Mnemonics.Select(fun f->f.QuoteOnDemand()) |> String.concat "; ") + ";"
                    let tab = getTab (indent+2)
                    let aliasKey =
                        match a.AliasTarget with
                        | Some(DuAliasTargetReal real) -> real.GetAliasTargetToDs(flow) |> getName
                        | Some(DuAliasTargetCall call) -> call.GetAliasTargetToDs() |> getName
                        | Some(DuAliasTargetRealExFlow rf) -> rf.Real.GetAliasTargetToDs(flow) |> getName
                        | None -> failwithlog "ERROR"

                    yield $"{tab}{aliasKey} = {lb} {mnemonics} {rb}"
                yield $"{tab}{rb}"

            yield $"{tab}{rb}"
        ] |> combineLines

    let codeBlockToDs (theSystem:DsSystem) =
        let vars = theSystem.Variables
        let tab = getTab 1
        let tab2 = getTab 2
        [
            if vars.Any() then
                yield $"{tab}[variables] = {lb}"
                for var in vars do
                    yield $"{tab2}{var.ToDsText()}"
                yield $"{tab}{rb}"
        ] |> combineLines

    let rec systemToDs (system:DsSystem) (indent:int) (printComment:bool)=
        pCooment <- printComment
        let tab = getTab indent
        let tab2 = getTab 2
        let tab3 = getTab 3
       
        let printFunc (func:Func) = $"${func.Name}"
        let addressPrint (addr:string) = if isNullOrEmpty  addr then TextAddrEmpty else addr
        [
            yield $"[sys] {system.Name.QuoteOnDemand()} = {lb}"

            for f in system.Flows do
                yield flowToDs f indent

            if system.Jobs.Any() then
                let printDev (ai:TaskDev) = $"{ai.ApiName}({addressPrint ai.InAddress}, {addressPrint ai.OutAddress})"
                yield $"{tab}[jobs] = {lb}"
                for c in system.Jobs do
                    let jobItems =
                        c.DeviceDefs
                        |> Seq.map printDev
                        |> fun devs -> match c.OperatorFunction with
                                        | Some f -> devs @ [printFunc f]
                                        | None -> devs
                          
                    let jobItemText =  jobItems.JoinWith("; ") + ";"
                    yield $"{tab2}{c.Name.QuoteOnDemand()} = {lb} {jobItemText} {rb}"  
                yield $"{tab}{rb}"

            let operators = system.Functions.OfType<OperatorFunction>()
            if operators.Any() then
                yield $"{tab}[operators] = {lb}"
                for op in operators do
                    if op.OperatorType = DuOPUnDefined
                    then 
                        yield $"{tab2}{op.Name};"
                    else 
                        yield $"{tab2}{op.Name} = {op.ToDsText()};"

                yield $"{tab}{rb}"

            let commands = system.Functions.OfType<CommandFunction>()
            if commands.Any() then
                yield $"{tab}[commands] = {lb}"
                for cmd in commands do
                    if cmd.CommandType = DuCMDUnDefined
                    then 
                        yield $"{tab2}{cmd.Name}{lb}{tab}{rb};"
                    else
                        let cmdArgs = cmd.ToDsText().Trim(';').Split(';')
                        if cmdArgs.length() > 1
                        then 
                            yield $"{tab2}{cmd.Name} = {lb}"
                            for arg in cmdArgs  
                                do yield $"{tab3}{arg};"
                            yield $"{tab2}{rb}"
                        else 
                            yield $"{tab2}{cmd.Name} = {lb}{cmd.ToDsText()}{rb};"

                yield $"{tab}{rb}"

            if system.ApiItems.Any() then
                yield $"{tab}[interfaces] = {lb}"
                for item in system.ApiItems do
                    let ser =
                        let getFlowAndRealName (r:Real) = [r.Flow.Name; r.Name].Combine()
                        let qNames (xs:Real seq) = xs.Select(getFlowAndRealName) |> String.concat(", ")
                        let coverWithUnderScore (x:string) = if x.IsNullOrEmpty() then "_" else x
                        let s = qNames(item.TXs) |> coverWithUnderScore
                        let e = qNames(item.RXs) |> coverWithUnderScore
                        $"{s} ~ {e}"
                    yield $"{tab2}{item.Name.QuoteOnDemand()} = {lb} {ser} {rb}"

                for ri in system.ApiResetInfos do
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
                [
                    if hws.length() > 0 then
                        yield $"{tab2}[{category}] = {lb}"
                        for hw in hws do
                            let flows = hw.SettingFlows.Select(fun f -> f.NameComponents.Skip(1).Combine().QuoteOnDemand())
                            let items = 
                                if hw.OperatorFunction.IsSome 
                                then [printFunc hw.OperatorFunction.Value]  @ flows else flows
                                
                            let itemText = if items.any() then (items |> String.concat "; ") + ";" else ""
                            let inAddr =  addressPrint  hw.InAddress  
                            let outAddr = addressPrint  hw.OutAddress 
                            yield $"{tab3}{hw.Name.QuoteOnDemand()}({inAddr}, {outAddr}) = {lb} {itemText} {rb}"
                          
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

            let withSafeties = safetyHolders.Where(fun h -> h.SafetyConditions.Any())
            let safeties =
                let getCallName (call:Call) =
                    match call.Parent with
                    | DuParentReal r-> $"{r.Flow.Name}.{call.ParentNPureNames.Combine()}"
                    | DuParentFlow _ -> call.ParentNPureNames.Combine()

                let safetyConditionName (sc:SafetyCondition) =
                    match sc with
                    | DuSafetyConditionReal real       -> real.ParentNPureNames.Combine()
                    | DuSafetyConditionCall call       -> getCallName call
                    | DuSafetyConditionRealExFlow rf   -> rf.ParentNPureNames.Combine()
                let safetyConditionHolderName(sch:ISafetyConditoinHolder) =
                    match sch with
                    | :? Real as real -> real.ParentNPureNames.Combine()
                    | :? Call as call -> getCallName call
                    | :? RealOtherFlow as realExF -> realExF.ParentNPureNames.Combine()
                    | _ -> failwithlog "ERROR"

                [
                    if withSafeties.Any() then
                        yield $"{tab2}[safety] = {lb}"
                        for safetyHolder in withSafeties do
                            let conds = safetyHolder.SafetyConditions.Select(safetyConditionName).JoinWith("; ") + ";"
                            yield $"{tab3}{safetyConditionHolderName safetyHolder} = {lb} {conds} {rb}"
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
                    if layoutList.any()
                    then

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



            let finishedReals =
                [
                    for flow in system.Flows do
                    for vert in flow.Graph.Vertices do
                        match vert with
                        | :? Real as real -> if real.Finished then yield real
                        | _ -> ()
                ]
            let finished = 
                [
                if finishedReals.Any() then
                    yield $"{tab2}[finish] = {lb}"
                    for real in finishedReals do
                        yield $"{tab3}{real.Flow.Name}.{real.Name};"
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
                        yield $"{tab3}{compo[1]}.{compo[2]}.{compo[3]};"
                    yield $"{tab2}{rb}"
                ] |> combineLines
            if safeties.Any() || layouts.Any() || finished.Any() || disabled.Any() then
                yield $"{tab}[prop] = {lb}"
                if safeties.Any()  then yield safeties
                if layouts.Any()   then yield layouts
                if finished.Any()  then yield finished
                if disabled.Any()  then yield disabled
                yield $"{tab}{rb}"
            let commentDevice(d:Device) = if pCooment then  $"// {d.AbsoluteFilePath}" else "";
            for d in system.Devices do
                yield $"{tab}[device file={quote d.RelativeFilePath}] {d.Name.QuoteOnDemand()}; {commentDevice d}"
            
            let commentSystem(es:ExternalSystem) = if pCooment then  $"// {es.AbsoluteFilePath}" else "";
            for es in system.ExternalSystems do
                yield $"{tab}[external file={quote es.RelativeFilePath}] {es.Name}; {commentSystem es}"

            //Commands/Observes는 JobDef에 저장 (Variables는 OriginalCodeBlocks ?? System.Variables ??)
            yield codeBlockToDs system

            // todo 복수개의 block 이 허용되면, serialize 할 때 해당 위치에 맞춰서 serialize 해야 하는데...
            for code in system.OriginalCodeBlocks do
                yield code

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
