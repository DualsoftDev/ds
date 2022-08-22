namespace Engine.Runner

open System.Linq
open System.Collections.Generic
open System.Runtime.CompilerServices

open Engine.Common
open Engine.Common.FS
open Engine.Core
open Engine.OPC
open Engine.Graph

[<AutoOpen>]
module internal ModelModule =
        /// rename flow/segment tags, add flow auto bit
    let renameBits(model:Model) =
        // root flow 를 cpu 별로 grouping
        let allRootFlows = model.Systems.selectMany(fun s -> s.RootFlows)
        let flowsGrps = allRootFlows.GroupByToDictionary(fun flow -> flow.Cpu)
        for (cpu, flows) in flowsGrps.Select(fun kv -> kv.ToTuple()) do
            let cpuBits = new HashSet<IBit>()
            for f in flows do
                for seg in f.RootSegments do
                    let q = seg.QualifiedName
                    for t in [seg.TagStart; seg.TagReset; seg.TagEnd; seg.Going; seg.Ready] do
                        cpuBits.Add(t) |> ignore
                        t.Name <- $"{t.InternalName}_{q}"

                    for p in [seg.PortS :> PortInfo; seg.PortR; seg.PortE;] do
                        cpuBits.Add(p) |> ignore
                        p.Name <- $"{p.InternalName}_{q}"
                        if p.Actual <> null then
                            cpuBits.Add(p.Actual) |> ignore
                            p.Actual.SetName $"{p.InternalName}_Actual_{q}"

                        cpuBits.Add(p.Plan) |> ignore
                        if p = seg.PortE then
                            p.Plan.SetName $"{p.InternalName}_Plan_{q}"

                cpuBits.Add f.Auto |> ignore
                f.Auto.Name <- $"Auto_{f.QualifiedName}"

            assert(cpuBits.ForAll(cpu.BitsMap.Values.Contains))
            assert(cpuBits.OfType<Tag>().ForAll(cpu.TagsMap.Values.Contains))

    /// Tag 이름 변경으로 인한, cpu 의 BitMap/TagsMap 갱신 및 OPC tag 갱신
    let rebuildMap(model:Model, opc:OpcBroker) =
        for cpu in model.Cpus do
            let cpuBits = cpu.BitsMap.Values.ToHashSet()
            let oldKeys = cpu.BitsMap.Where(fun kv -> cpuBits.Contains(kv.Value)).Select(fun kv -> kv.Key).ToArray()
            for ok in oldKeys do
                cpu.BitsMap.Remove(ok) |> ignore
                cpu.TagsMap.Remove(ok) |> ignore

            for b in cpuBits do
                let n = b.GetName()
                cpu.BitsMap.Add(n, b)
                match b with
                | :? Tag as t ->
                    cpu.TagsMap.Add(n, t)
                | _ ->
                    ()

            opc.AddTags(cpuBits.OfType<Tag>())

    let markChildren(model:Model) =
        let allRootSegments =
            model.Systems.selectMany(fun s -> s.RootFlows)
                .SelectMany(fun f -> f.RootSegments)
        let children =
                allRootSegments
                    .SelectMany(fun rs -> rs.Children)
                    .Distinct()
        for ch in children do
            let getTags(x:ITxRx, tx:bool) =
                match x with
                    | :? SegmentBase as seg -> if tx then seg.TagStart else seg.TagEnd
                    | :? Tag as tag -> tag
                    | _ -> failwith "ERROR"
            match ch.Coin with
                | :? Call as call->
                    ch.TagsStart <- call.Prototype.TXs.Select(fun tx -> getTags(tx, true)) |> ResizeArray
                    ch.TagsEnd   <- call.Prototype.RXs.Select(fun rx -> getTags(rx, false)) |> ResizeArray
                    for ts in ch.TagsStart do
                        ts.Type <- ts.Type ||| TagType.TX ||| TagType.External
                    for te in ch.TagsEnd do
                        te.Type <- te.Type ||| TagType.RX ||| TagType.External
                | :? ExSegmentCall as exSegCall->
                    let ex = exSegCall.ExternalSegment
                    ch.TagsStart <- [ex.TagStart] |> ResizeArray
                    ch.TagReset <- ex.TagReset
                    ch.TagsEnd <- [ex.TagEnd] |> ResizeArray

                    for t in ch.TagsStart @@ ch.TagsEnd do
                        t.Type <- t.Type ||| TagType.External
                    ch.TagReset.Type <- ch.TagReset.Type ||| TagType.External
                | _ ->
                    failwith "ERROR"

        let activeFlowRoots =
            model.Cpus.First(fun cpu -> cpu.IsActive)
                .RootFlows.SelectMany(fun f -> f.RootSegments)
        for seg in activeFlowRoots do
            for t in [seg.TagStart; seg.TagReset; seg.TagEnd] do
                t.Type <- t.Type ||| TagType.External
                

[<Extension>] // type Segment =
type ModelExt =
    [<Extension>]
    static member Epilogue(model:Model, opc:OpcBroker) =
        markChildren(model)
        //markTxRxTags(model)
        let segments =
            model.Systems.selectMany(fun sys -> sys.RootFlows)
                .selectMany(fun rf -> rf.RootSegments)
        for segment in segments do
            segment.Epilogue()

        renameBits(model)
        rebuildMap(model, opc)


    [<Extension>]
    static member BuildGraphInfo(model:Model) =
        
        let rootFlows = model.Systems.selectMany(fun sys -> sys.RootFlows)
        for flow in rootFlows do
            flow.GraphInfo <- FsGraphInfo.AnalyzeFlows([flow], true);

        for cpu in model.Cpus do
            cpu.GraphInfo <- FsGraphInfo.AnalyzeFlows(cpu.RootFlows.Cast<Flow>(), true)

        let segments = rootFlows.SelectMany(fun rf -> rf.RootSegments).Cast<Segment>()
        for seg in segments do
            seg.GraphInfo <- FsGraphInfo.AnalyzeFlows([seg], false)
            let pi = new GraphProgressSupportUtil.ProgressInfo(seg.GraphInfo)
            seg.ChildrenOrigin <- pi.ChildOrigin.ToArray();


