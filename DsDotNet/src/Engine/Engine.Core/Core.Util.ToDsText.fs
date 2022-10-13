namespace Engine.Core

open System.Runtime.CompilerServices
open System.Linq
open Engine.Common.FS

[<AutoOpen>]
module internal ToDsTextModule =
    let getTab n = Seq.init n (fun _ -> "    ") |> String.concat ""
    let lb, rb = "{", "}"
    let combineLines = ofNotNullAny >> joinLines

    let segmentToDs (segmentBase:SegmentBase) (indent:int) =
        let tab = getTab indent
        let tab2 = getTab (indent + 1)
        [
            match segmentBase with
            | :? Segment as segment ->
                let ess = groupDuplexEdges segment.Graph
                if ess.Any() then
                    yield $"{tab}{segment.Name} = {lb}"
                    for KeyValue(_, es) in ess do
                        for e in es do
                            yield $"{tab2}{e.Source.Name} {e.EdgeType.ToText()} {e.Target.Name};"
                    yield $"{tab}{rb}"
                //for v in segment.Graph.Vertices do
                //    ()
            | :? SegmentAlias as ali ->
                ()//SegmentAlias.Create(ali.Name, targetFlow, ali.AliasKey)
            | :? SegmentApiCall as call ->
                //let apiItem = copyApiItem(call.ApiItem)
                //SegmentApiCall.Create(apiItem, targetFlow)
                ()
            | _ ->
                failwith "ERROR"
        ] |> combineLines

    let flowGraphToDs (graph:Graph<SegmentBase, InFlowEdge>) (indent:int) =
        let tab = getTab indent
        let ess = groupDuplexEdges graph
        [
            for KeyValue(_, es) in ess do
                for e in es do
                    yield $"{tab}{e.Source.Name} {e.EdgeType.ToText()} {e.Target.Name};"

            for v in graph.Vertices do
                yield segmentToDs v indent

            let islands = graph.Islands
            for island in islands do
                yield $"{tab}{island.Name};"
        ] |> combineLines

    let flowToDs (flow:Flow) (indent:int) =
        let tab = getTab indent
        [
            yield $"{tab}[flow] {flow.Name} = {lb}"
            yield flowGraphToDs flow.Graph (indent+1)

            let alias = flow.AliasMap
            if alias.Count > 0 then
                let tab = getTab (indent+1)
                yield $"{tab}[aliases] = {lb}"
                for KeyValue(k, v) in alias do
                    let mnemonics = (v |> String.concat "; ") + ";"
                    let tab = getTab (indent+2)
                    yield $"{tab}{k.Combine()} = {lb} {mnemonics} {rb}"
                yield $"{tab}{rb}"
                        
            yield $"{tab}{rb}"
        ] |> combineLines

    let systemToDs (system:DsSystem) =
        [
            let ip = if system.Host <> null then $" ip = {system.Host}" else ":"
            yield $"[sys{ip}] {system.Name} = {lb}"
            let indent = 1

            for f in system.Flows do
                yield flowToDs f indent

            let tab = getTab indent
            let tab2 = getTab (indent+1)

            let api = system.Api
            if api <> null then
                yield $"{tab}[interfaces] = {lb}"
                for item in api.Items do
                    let ser =
                        let ifnull (onNull:string) (x:string) = if x.isNullOrEmpty() then onNull else x
                        let qNames (xs:Segment seq) = xs.Select(fun tx -> tx.QualifiedName) |> String.concat(", ")
                        let s = qNames(item.TXs) |> ifnull "_"
                        let e = qNames(item.RXs) |> ifnull "_"
                        let r = qNames(item.Resets)
                        if r.isNullOrEmpty() then $"{s} ~ {e}" else $"{s} ~ {e} ~ {r}"
                    yield $"{tab2}{item.Name.QuoteOnDemand()} = {lb} {ser} {rb}"

                for ri in api.ResetInfos do
                    yield $"{tab2}{ri.Operand1} {ri.Operator} {ri.Operand2};"
                    
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

            yield rb
        ] |> combineLines

    let modelToDs (model:Model) =
        let tab = getTab 1
        let tab2 = getTab 2
        [
            for s in model.Systems do
                yield systemToDs s

            // prop
            //      safety
            //      addresses
            //      layouts
            let spits = model.Spit()
            let segs = spits.Select(fun spit -> spit.Obj).OfType<Segment>().ToArray()

            let withSafeties = segs.Where(fun seg -> seg.SafetyConditions.Any())
            let safeties =
                [
                    if withSafeties.Any() then
                        yield $"{tab}[safety] = {lb}"
                        for seg in withSafeties do
                            let conds = seg.SafetyConditions.Select(fun seg -> seg.QualifiedName).JoinWith("; ") + ";"
                            yield $"{tab2}{seg.QualifiedName} = {lb} {conds} {rb}"
                        yield $"{tab}{rb}"
                ] |> combineLines

            let withAddresses = segs.Where(fun seg -> seg.Addresses <> null)
            let addresses =
                [
                    if withAddresses.Any() then
                        yield $"{tab}[addresses] = {lb}"
                        for seg in withAddresses do
                            let ads = seg.Addresses
                            
                            yield $"{tab2}{seg.QualifiedName} = ( {ads.Start}, {ads.End}, {ads.Reset} )"
                        yield $"{tab}{rb}"

                ] |> combineLines

            let withLayouts =
                model.Systems
                    .Where(fun sys -> sys.Api <> null)
                    .SelectMany(fun sys -> sys.Api.Items.Where(fun ai -> ai.Xywh <> null))
                    ;
            let layouts =
                [
                    if withLayouts.Any() then
                        yield $"{tab}[layouts] = {lb}"
                        for apiItem in withLayouts do
                            let xywh = apiItem.Xywh
                            let posi =
                                if xywh.W.HasValue then
                                    $"({xywh.X}, {xywh.Y}, {xywh.W.Value}, {xywh.H.Value})"
                                else
                                    $"({xywh.X}, {xywh.Y})"
                            yield $"{tab2}{apiItem.QualifiedName} = {posi}"
                            
                        yield $"{tab}{rb}"
                ] |> combineLines

            if safeties.Any() || addresses.Any() || layouts.Any() then
                yield $"[prop] = {lb}"
                if safeties.Any()  then yield safeties
                if addresses.Any() then yield addresses
                if layouts.Any()   then yield layouts
                yield rb
        ] |> combineLines


[<Extension>]
type ToDsTextModuleHelper =
    [<Extension>] static member ToDsText(model:Model) = modelToDs(model)
    [<Extension>] static member ToDsText(system:DsSystem) = systemToDs(system)

