namespace Engine.Core

open System.Runtime.CompilerServices
open System.Linq
open Engine.Common.FS

[<AutoOpen>]
module internal ToDsTextModule =
    let getTab n = Seq.init n (fun _ -> "    ") |> String.concat ""
    let lb, rb = "{", "}"
    let joinLines xs = xs |> String.concat "\r\n"

    let segmentToDs (segmentBase:SegmentBase) (indent:int) =
        [
            match segmentBase with
            | :? Segment as segment ->
                for v in segment.Graph.Vertices do
                    ()
            | :? SegmentAlias as ali ->
                ()//SegmentAlias.Create(ali.Name, targetFlow, ali.AliasKey)
            | :? SegmentApiCall as call ->
                //let apiItem = copyApiItem(call.ApiItem)
                //SegmentApiCall.Create(apiItem, targetFlow)
                ()
            | _ ->
                failwith "ERROR"
        ]

    let flowGraphToDs (graph:Graph<SegmentBase, InFlowEdge>) (indent:int) =
        [
            for v in graph.Vertices do
                yield! segmentToDs v indent
        ] |> joinLines

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
        ] |> joinLines

    let systemToDs (system:DsSystem) =
        [
            let ip = if system.Host <> null then $" ip = {system.Host}" else ":"
            yield $"[sys{ip}] {system.Name} = {lb}"
            let indent = 1

            for f in system.Flows do
                yield flowToDs f indent

            let tab = getTab indent

            let api = system.Api
            if api <> null then
                yield $"{tab}[interfaces] = {lb}"
                for item in api.Items do
                    let tab = getTab (indent+1)
                    let ser =
                        let ifnull (onNull:string) (x:string) = if x.isNullOrEmpty() then onNull else x
                        let s = item.TXs.Select(fun tx -> tx.QualifiedName) |> String.concat(", ") |> ifnull "_"
                        let e = item.RXs.Select(fun rx -> rx.QualifiedName) |> String.concat(", ") |> ifnull "_"
                        let r = item.Resets.Select(fun rx -> rx.QualifiedName) |> String.concat(", ")
                        if r.isNullOrEmpty() then $"{s} ~ {e}" else $"{s} ~ {e} ~ {r}"

                    yield $"{tab}{item.Name.QuoteOnDemand()} = {lb} {ser} {rb}"
                yield $"{tab}{rb}"

            yield rb
        ] |> joinLines

    let modelToDs (model:Model) =
        [
            for s in model.Systems do
                systemToDs s
        ] |> joinLines


[<Extension>]
type ToDsTextModuleHelper =
    [<Extension>] static member ToDsText(model:Model) = modelToDs(model)

