namespace Engine.Core

open System.Linq
open Engine.Common.FS
open System.Runtime.CompilerServices

[<AutoOpen>]
module internal ToDsTextModule =
    let getTab n = Seq.init n (fun _ -> "    ") |> String.concat ""
    let lb, rb = "{", "}"
    let combineLines = ofNotNullAny >> joinLines


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
        let getName (v:Vertex) = getRawName v.PureNames true
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
                    yield ("\t\t// " + comments.JoinWith("") + ";")
                ] |> String.concat ""
        ]

    let rec graphToDs (container:ParentWrapper) (indent:int) =
        let tab = getTab indent
        let graph = container.GetGraph()
        let core = container.GetCore()
        [
            yield! modelingEdgeInfosToDs (container.GetModelingEdges()) tab

            let stems = graph.Vertices.OfType<Real>().Where(fun r -> r.Graph.Vertices.Any()).ToArray()
            for stem in stems do
                yield $"{tab}{stem.Name.QuoteOnDemand()} = {lb}"
                yield! graphToDs (ParentReal stem) (indent + 1)
                yield $"{tab}{rb}"

            let notMentioned = graph.Islands.Except(stems.Cast<Vertex>()).ToArray()
            for island in notMentioned do
                yield $"{getTab (indent+1)}{island.Name.QuoteOnDemand()}; // island"
        ]

    let flowToDs (flow:Flow) (indent:int) =
        let tab = getTab indent
        [
            yield $"{tab}[flow] {flow.Name.QuoteOnDemand()} = {lb}"
            yield! graphToDs (ParentFlow flow) (indent+1)

            let aliasDefs = flow.AliasDefs.Values
            if aliasDefs.Any() then
                let tab = getTab (indent+1)
                yield $"{tab}[aliases] = {lb}"
                for a in aliasDefs do
                    let mnemonics = (a.Mnemonincs |> String.concat "; ") + ";"
                    let tab = getTab (indent+2)
                    let aliasKey =
                        match a.AliasTarget with
                        | Some(AliasTargetReal real) -> real.GetAliasTargetToDs(flow).Combine()
                        | Some(AliasTargetCall call) -> call.GetAliasTargetToDs().Combine()
                        | Some(AliasTargetRealEx o) -> o.Real.GetAliasTargetToDs(flow).Combine()
                        | None -> failwith "ERROR"

                    yield $"{tab}{aliasKey} = {lb} {mnemonics} {rb}"
                yield $"{tab}{rb}"

            yield $"{tab}{rb}"
        ] |> combineLines

    let codeBlockToDs (theSystem:DsSystem) =
        let funApp (funApp:FunctionApplication) =
            let pgs (argGroups:ParameterGroup seq) =
                argGroups.Select(fun ag -> ag.JoinWith ", ")
                    .JoinWith " ~ "
            $"{funApp.FunctionName} = {pgs funApp.ParameterGroups}"
        let vars = theSystem.Variables
        let cmds = theSystem.Commands
        let obss = theSystem.Observes
        let tab = getTab 1
        let tab2 = getTab 2
        [
            if vars.Any() then
                yield $"{tab}[variables] = {lb}"
                for var in vars do
                    yield $"{tab2}{var.Name} = ({var.Type}, {var.InitValue})"
                yield $"{tab}{rb}"
            if cmds.Any() then
                yield $"{tab}[commands] = {lb}"
                for cmd in cmds do
                    yield $"{tab2}{cmd.Name} = (@{funApp cmd.FunctionApplication})"
                yield $"{tab}{rb}"
            if obss.Any() then
                yield $"{tab}[observes] = {lb}"
                for obs in obss do
                    yield $"{tab2}{obs.Name} = (@{funApp obs.FunctionApplication})"
                yield $"{tab}{rb}"
        ] |> combineLines

    let rec systemToDs (system:DsSystem) (indent:int) =
        let tab = getTab indent
        [
            let ip = if system.Host.IsNullOrEmpty() then "" else $" ip = {system.Host}"
            yield $"[sys{ip}] {system.Name.QuoteOnDemand()} = {lb}"

            for f in system.Flows do
                yield flowToDs f indent

            let tab2 = getTab (indent+1)

            if system.Jobs.Any() then
                let print (ai:JobDef) = $"{ai.ApiName}({ai.OutTag}, {ai.InTag})"
                yield $"{tab}[jobs] = {lb}"
                for c in system.Jobs do
                    let ais = c.JobDefs.Select(print).JoinWith("; ") + ";"
                    yield $"{tab2}{c.Name.QuoteOnDemand()} = {lb} {ais} {rb}"
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




            let buttonsToDs(category:string, btns:ButtonDic) =
                [
                    if btns.Count > 0 then
                        yield $"{tab}[{category}] = {lb}"
                        for KeyValue(k, v) in btns do
                            let flows = (v.Select(fun f -> f.NameComponents.Skip(1).Combine()) |> String.concat ";") + ";"
                            yield $"{tab2}{k} = {lb} {flows} {rb}"
                        yield $"{tab}{rb}"
                ] |> combineLines
            yield buttonsToDs("auto" , system.AutoButtons)
            yield buttonsToDs("emg"  , system.EmergencyButtons)
            yield buttonsToDs("start", system.StartButtons)
            yield buttonsToDs("reset", system.ResetButtons)

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
                    |ParentReal r-> $"{r.Flow.Name}.{call.ParentNPureNames.Combine()}"       
                    |ParentFlow f-> call.ParentNPureNames.Combine()

                let safetyConditionName (sc:SafetyCondition) =
                    match sc with
                    | SafetyConditionReal real -> real.ParentNPureNames.Combine()
                    | SafetyConditionCall call -> getCallName call
                    | SafetyConditionRealEx  o -> o.ParentNPureNames.Combine()
                let safetyConditionHolderName(sch:ISafetyConditoinHolder) =
                    match sch with
                    | :? Real as real -> real.ParentNPureNames.Combine()
                    | :? Call as call -> getCallName call
                    | :? RealOtherFlow as realEx -> realEx.ParentNPureNames.Combine()
                    | _ -> failwith "ERROR"

                [
                    if withSafeties.Any() then
                        yield $"{tab2}[safety] = {lb}"
                        for safetyHolder in withSafeties do
                            let conds = safetyHolder.SafetyConditions.Select(safetyConditionName).JoinWith("; ") + ";"
                            yield $"{tab2}{safetyConditionHolderName safetyHolder} = {lb} {conds} {rb}"
                        yield $"{tab2}{rb}"
                ] |> combineLines

            let calls =
                [   for f in system.Flows do
                        yield! f.Graph.Vertices.OfType<Call>()
                ] |> List.distinct

            let withLayouts = calls.Where(fun call -> call.Xywh <> null)
            let layouts =
                [
                    if withLayouts.Any() then
                        yield $"{tab2}[layouts] = {lb}"
                        for call in withLayouts do
                            let xywh = call.Xywh
                            let posi =
                                if xywh.W.HasValue then
                                    $"({xywh.X}, {xywh.Y}, {xywh.W.Value}, {xywh.H.Value})"
                                else
                                    $"({xywh.X}, {xywh.Y})"
                            yield $"{tab2}{call.Name} = {posi}"

                        yield $"{tab2}{rb}"
                ] |> combineLines

            if safeties.Any() || layouts.Any() then
                yield $"{tab}[prop] = {lb}"
                if safeties.Any()  then yield safeties
                if layouts.Any()   then yield layouts
                yield $"{tab}{rb}"

            for d in system.Devices do          yield $"{tab}[device file={quote d.UserSpecifiedFilePath}] {d.Name}; // {d.AbsoluteFilePath}"
            for es in system.ExternalSystems do  yield $"{tab}[external file={quote es.UserSpecifiedFilePath}] {es.Name}; // {es.AbsoluteFilePath}"


            yield codeBlockToDs system




            yield rb
        ] |> combineLines

    type DsSystem with
        member x.ToDsText() = systemToDs x 1

           
[<Extension>]
type SystemExt =  
    [<Extension>] static member ToDsText (system:DsSystem) = systemToDs system 1
