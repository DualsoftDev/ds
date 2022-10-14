namespace Engine.Core

open System.Runtime.CompilerServices
open System.Linq
open Engine.Common.FS

[<AutoOpen>]
module internal ToDsTextModule =
    let getTab n = Seq.init n (fun _ -> "    ") |> String.concat ""
    let lb, rb = "{", "}"
    let combineLines = ofNotNullAny >> joinLines

    let rec graphEntitiesToDs<'V when 'V :> INamed and 'V : equality> (vertices:'V seq) (edges:EdgeBase<'V> seq) (indent:int) =
        let tab = getTab indent
        [
            let startEdges = edges.OfNotStrongResetEdge().ToArray()
            for e in startEdges do
                yield $"{tab}{e.ToText()};"
            let resetEdges = edges.OfStrongResetEdge().ToArray()
            let ess = groupDuplexEdges resetEdges
            for KeyValue(_, es) in ess do
                let es = es.ToArray()
                if es.Length = 2 then
                    assert(es[0].EdgeType.HasFlag(EdgeType.AugmentedTransitiveClosure) = es[1].EdgeType.HasFlag(EdgeType.AugmentedTransitiveClosure))
                    assert(es[0].Source = es[1].Target && es[0].Target = es[1].Source)
                    let commentOnAugmented = if es[0].EdgeType.HasFlag(EdgeType.AugmentedTransitiveClosure) then "//" else ""
                    yield $"{tab}{commentOnAugmented}{es[0].Source.Name} <||> {es[0].Target.Name};"
                else
                    assert(es.Length = 1)
                    yield $"{tab}{es[0].ToText()};"

            for v in vertices.OfType<Segment>() do
                yield vertexToDs v indent

            let islands = vertices.Except(edges.selectMany(fun e -> e.GetVertices()))
            for island in islands do
                yield $"{tab}{island.Name}; // island"
        ] |> combineLines

    and vertexToDs (vertex:INamed) (indent:int) =
        let tab = getTab indent
        [
            match vertex with
            | :? Segment as segment ->
                let subGraph = segment.Graph
                if subGraph.Edges.any() then
                    yield $"{tab}{segment.Name} = {lb}"
                    let es = subGraph.Edges.Cast<EdgeBase<Child>>().ToArray()
                    let vs = subGraph.Vertices
                    yield graphEntitiesToDs vs es (indent+1)
                    yield $"{tab}{rb}"

            //| :? SegmentAlias 
            //| :? SegmentApiCall ->
            //    ()
            | _ ->
                failwith "ERROR"
        ] |> combineLines



    let flowGraphToDs (graph:Graph<SegmentBase, InFlowEdge>) (indent:int) =
        let es = graph.Edges.OfType<EdgeBase<SegmentBase>>().ToArray()
        graphEntitiesToDs graph.Vertices es indent

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

