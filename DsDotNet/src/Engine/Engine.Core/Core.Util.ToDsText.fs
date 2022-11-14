namespace Engine.Core

open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open System.Linq
open Engine.Common.FS

[<AutoOpen>]
module internal ToDsTextModule =
    let getTab n = Seq.init n (fun _ -> "    ") |> String.concat ""
    let lb, rb = "{", "}"
    let combineLines = ofNotNullAny >> joinLines


    type private MEI = ModelingEdgeInfo<Vertex>
    let private modelingEdgeInfosToDs (es:MEI seq) (basis:Fqdn) (tab:string) =

        (* rss : Result edge Set of Set *)
        let folder (rss:ResizeArray<MEI> list) (e:MEI) : ResizeArray<MEI> list =
            if rss.IsEmpty then
                [[e] |> ResizeArray]
            else
                match rss.TryFind(fun rs -> rs[0].Source = e.Target) with
                | Some rs -> rs.Insert(0, e); rss
                | _ ->
                    match rss.TryFind(fun rs -> rs.Last().Target = e.Source) with
                    | Some rs -> rs.Add(e); rss
                    | _ ->
                        ([e] |> ResizeArray)::rss


        let es  = es |> Seq.sortBy(fun e -> e.EdgeSymbol.Count(fun ch -> ch = '|'))
        let ess = es |> Seq.fold folder []

        [
            for es in ess do
                [
                    yield $"{tab}"
                    for i, e in es.Indexed() do
                        let mutable s = e.Source.GetRelativeName(basis)
                        let t = e.Target.GetRelativeName(basis)
                        if i <> 0 then s <- ""
                        yield $"{s} {e.EdgeSymbol} {t}"
                    yield ";"
                ] |> String.concat ""
        ]

    let rec graphToDs (container:ParentWrapper) (indent:int) =
        let tab = getTab indent
        let graph = container.GetGraph()
        let core = container.GetCore()
        let basis = core.NameComponents
        [
            yield! modelingEdgeInfosToDs (container.GetModelingEdges()) basis tab

            let stems = graph.Vertices.OfType<Real>().Where(fun r -> r.Graph.Vertices.Any()).ToArray()
            for stem in stems do
                yield $"{tab}{stem.Name.QuoteOnDemand()} = {lb}"
                yield! graphToDs (Real stem) (indent + 1)
                yield $"{tab}{rb}"

            let notMentioned = graph.Islands.Except(stems.Cast<Vertex>()).ToArray()
            for island in notMentioned do
                yield $"{getTab (indent+1)}{island.GetRelativeName(core.NameComponents)}; // island"
        ]

    let flowToDs (flow:Flow) (indent:int) =
        let tab = getTab indent
        [
            yield $"{tab}[flow] {flow.Name.QuoteOnDemand()} = {lb}"
            yield! graphToDs (Flow flow) (indent+1)

            let alias = flow.AliasMap
            if alias.Any() then
                let tab = getTab (indent+1)
                yield $"{tab}[aliases] = {lb}"
                for KeyValue(k, v) in alias do
                    let mnemonics = (v |> String.concat "; ") + ";"
                    let tab = getTab (indent+2)
                    yield $"{tab}{k.Combine()} = {lb} {mnemonics} {rb}"
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
        [
            if vars.Any() then
                yield "[variables] = {"
                for var in vars do
                    yield $"    {var.Name} = @({var.Type}, {var.InitValue})"
                yield "}"
            if cmds.Any() then
                yield "[commands] = {"
                for cmd in cmds do
                    yield $"    {cmd.Name} = @({funApp cmd.FunctionApplication})"
                yield "}"
            if obss.Any() then
                yield "[observes] = {"
                for obs in obss do
                    yield $"    {obs.Name} = @({funApp obs.FunctionApplication})"
                yield "}"
        ] |> combineLines

    let rec systemToDs (system:DsSystem) (indent:int) =
        let tab = getTab indent
        [
            let ip = if system.Host.IsNullOrEmpty() then "" else $" ip = {system.Host}"
            yield $"{tab}[sys{ip}] {system.Name} = {lb}"

            for f in system.Flows do
                yield flowToDs f indent


            let tab = getTab indent
            let tab2 = getTab (indent+1)

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
                    addresses *)
            let addresses =
                [
                    for KeyValue(apiPath, address) in system.ApiAddressMap do
                        yield $"{tab2}{apiPath.Combine()} = ( {address.In}, {address.Out})"
                ] |> combineLines
            if addresses.Any() then
                yield $"[prop] = {lb}"
                yield $"{tab}[addresses] = {lb}"
                yield addresses
                yield $"{tab}{rb}"
                yield rb


            (* prop
                    safety
                    layouts *)
            let spits = system.Spit()
            let segs = spits.Select(fun spit -> spit.GetCore()).OfType<Real>().ToArray()

            let withSafeties = segs.Where(fun seg -> seg.SafetyConditions.Any())
            let safeties =
                [
                    if withSafeties.Any() then
                        yield $"{tab}[safety] = {lb}"
                        for seg in withSafeties do
                            let getSegmentPath (seg:Real) = getRelativeName [|system.Name|] seg.NameComponents
                            let conds = seg.SafetyConditions.Select(getSegmentPath).JoinWith("; ") + ";"
                            yield $"{tab2}{seg.QualifiedName} = {lb} {conds} {rb}"
                        yield $"{tab}{rb}"
                ] |> combineLines

            let withLayouts = system.ApiItems.Where(fun ai -> ai.Xywh <> null)
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

            if safeties.Any() || layouts.Any() then
                yield $"[prop] = {lb}"
                if safeties.Any()  then yield safeties
                if layouts.Any()   then yield layouts
                yield rb




            yield codeBlockToDs system




            yield rb
        ] |> combineLines


    //let modelToDs (model:Model) =
    //    let tab = getTab 1
    //    let tab2 = getTab 2
    //    [
    //        for s in model.Systems.OrderBy(fun s -> not s.Active) do//mySystem 부터 출력
    //            yield systemToDs s

    //        yield codeBlockToDs model

    //        (* prop
    //             safety
    //             layouts *)
    //        let spits = model.Spit()
    //        let segs = spits.Select(fun spit -> spit.GetCore()).OfType<Real>().ToArray()

    //        let withSafeties = segs.Where(fun seg -> seg.SafetyConditions.Any())
    //        let safeties =
    //            [
    //                if withSafeties.Any() then
    //                    yield $"{tab}[safety] = {lb}"
    //                    for seg in withSafeties do
    //                        let conds = seg.SafetyConditions.Select(fun seg -> seg.QualifiedName).JoinWith("; ") + ";"
    //                        yield $"{tab2}{seg.QualifiedName} = {lb} {conds} {rb}"
    //                    yield $"{tab}{rb}"
    //            ] |> combineLines

    //        let withLayouts =
    //            model.Systems
    //                .SelectMany(fun sys -> sys.ApiItems.Where(fun ai -> ai.Xywh <> null))
    //                ;
    //        let layouts =
    //            [
    //                if withLayouts.Any() then
    //                    yield $"{tab}[layouts] = {lb}"
    //                    for apiItem in withLayouts do
    //                        let xywh = apiItem.Xywh
    //                        let posi =
    //                            if xywh.W.HasValue then
    //                                $"({xywh.X}, {xywh.Y}, {xywh.W.Value}, {xywh.H.Value})"
    //                            else
    //                                $"({xywh.X}, {xywh.Y})"
    //                        yield $"{tab2}{apiItem.QualifiedName} = {posi}"

    //                    yield $"{tab}{rb}"
    //            ] |> combineLines

    //        if safeties.Any() || layouts.Any() then
    //            yield $"[prop] = {lb}"
    //            if safeties.Any()  then yield safeties
    //            if layouts.Any()   then yield layouts
    //            yield rb
    //    ] |> combineLines


[<Extension>]
type ToDsTextModuleHelper =
    //[<Extension>] static member ToDsText(model:Model) = modelToDs(model)
    [<Extension>] static member ToDsText(system:DsSystem, [<Optional; DefaultParameterValue(1)>]indent) = systemToDs system indent

