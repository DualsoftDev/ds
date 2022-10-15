namespace Engine.Core

open System.Runtime.CompilerServices
open System.Linq
open Engine.Common.FS
open System.Collections.Generic
open GraphModule

[<AutoOpen>]
module internal ToDsTextModule =
    let getTab n = Seq.init n (fun _ -> "    ") |> String.concat ""
    let lb, rb = "{", "}"
    let combineLines = ofNotNullAny >> joinLines

    /// Edge 를 최대한 한줄로 세운 것을 우선으로 출력하고, 나머지 대충 출력
    let edgesToDs<'V when 'V :> FqdnObject and 'V : equality>
            (baseNameComponents:NameComponents) (edges:EdgeBase<'V> seq) (indent:int) =
        let gr = Graph(Seq.empty, edges)    // 계산용 graph
        let processed = HashSet<EdgeBase<'V>>()
        let inits, vs = gr.Inits, gr.Vertices

        /// v 에서 시작하는 chain edges 찾기
        let rec chainFrom (results:EdgeBase<'V> list) v : EdgeBase<'V> list list =
            [
                let es = gr.GetOutgoingEdges(v).Where(processed.Contains >> not).ToArray()
                let mutable res = results
                if es.Any() then
                    for e in es do
                        processed.Add(e) |> ignore
                        yield! chainFrom (e::res) e.Target
                        res <- []
                else
                    yield res |> List.rev
            ]

        /// 주어진 edge 로 임시 생성한 graph 의 init 에서부터 chain 을 구해서 출력
        let tab = getTab indent
        [
            for i in inits do
            for chain in chainFrom [] i do
                (*
                   (A -> B) -> (B -> C)
                   => "A -> " + "B -> " + "C"
                   => "A -> B -> C";
                *)
                let chained = chain.Select(fun e -> $"{e.Source.NameComponents.GetRelativeName(baseNameComponents)} {e.EdgeType.ToText()} ").JoinWith("")
                yield $"{tab}{chained}{chain.Last().Target.NameComponents.GetRelativeName(baseNameComponents)};"
        ]

    let rec graphEntitiesToDs<'V when 'V :> FqdnObject and 'V : equality>
        (baseNameComponents:NameComponents) (vertices:'V seq) (edges:EdgeBase<'V> seq) (indent:int) =

        let tab = getTab indent
        [
            // start 인과(reset 인과 아닌 것) 먼저 출력
            let startEdges = edges.OfNotResetEdge().ToArray()
            yield! edgesToDs baseNameComponents startEdges indent

            let startEdges = edges.OfWeakResetEdge().ToArray()
            yield! edgesToDs baseNameComponents startEdges indent

            let resetEdges = edges.OfStrongResetEdge().ToArray()
            let ess = groupDuplexEdges resetEdges
            for KeyValue(_, es) in ess do
                let es = es.ToArray()
                if es.Length = 2 then
                    assert(es[0].EdgeType.HasFlag(EdgeType.AugmentedTransitiveClosure) = es[1].EdgeType.HasFlag(EdgeType.AugmentedTransitiveClosure))
                    assert(es[0].Source = es[1].Target && es[0].Target = es[1].Source)
                    let commentOnAugmented = if es[0].EdgeType.HasFlag(EdgeType.AugmentedTransitiveClosure) then "//" else ""
                    yield $"{tab}{commentOnAugmented}{es[0].Source.NameComponents.GetRelativeName(baseNameComponents)} <||> {es[0].Target.NameComponents.GetRelativeName(baseNameComponents)};"
                else
                    assert(es.Length = 1)
                    yield $"{tab}{es[0].ToText()};"

            let segments = vertices.OfType<Segment>().ToArray()
            for v in segments do
                yield segmentToDs baseNameComponents v indent

            let islands =
                vertices
                    .Where(fun v -> (box v) :? Segment &&  not <| segments.Contains( (box v) :?> Segment))
                    .Except((*segments @@*) edges.Collect(fun e -> e.GetVertices()))
            for island in islands do
                yield $"{tab}{island.NameComponents.GetRelativeName(baseNameComponents)}; // island"
        ] |> combineLines

    and segmentToDs (baseNameComponents:NameComponents) (segment:Segment) (indent:int) =
        let tab = getTab indent
        [
            //let baseNameComponents = segment.NameComponents
            let subGraph = segment.Graph
            if subGraph.Edges.any() then
                yield $"{tab}{segment.NameComponents.GetRelativeName(baseNameComponents)} = {lb}"
                let es = subGraph.Edges.Cast<EdgeBase<Child>>().ToArray()
                let vs = subGraph.Vertices
                yield graphEntitiesToDs segment.NameComponents vs es (indent+1)
                yield $"{tab}{rb}"
        ] |> combineLines

    let flowGraphToDs (flow:Flow) (indent:int) =
        let graph = flow.Graph
        let baseNameComponents = flow.NameComponents
        let es = graph.Edges.OfType<EdgeBase<SegmentBase>>().ToArray()
        graphEntitiesToDs baseNameComponents graph.Vertices es indent

    let flowToDs (flow:Flow) (indent:int) =
        let tab = getTab indent
        [
            yield $"{tab}[flow] {flow.Name.QuoteOnDemand()} = {lb}"
            yield flowGraphToDs flow (indent+1)

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
            let ip = if system.Host <> null then $" ip = {system.Host}" else ""
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
                        let ifnull (onNull:string) (x:string) = if x.IsNullOrEmpty() then onNull else x
                        let qNames (xs:Segment seq) = xs.Select(fun tx -> tx.QualifiedName) |> String.concat(", ")
                        let s = qNames(item.TXs) |> ifnull "_"
                        let e = qNames(item.RXs) |> ifnull "_"
                        let r = qNames(item.Resets)
                        if r.IsNullOrEmpty() then $"{s} ~ {e}" else $"{s} ~ {e} ~ {r}"
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
        let xx = "a\nB\nc\na\nb\n".SplitByLine()
        let yy = "a\nB\nc\na\nb\n".Split([|'\r'; '\n'|])

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

