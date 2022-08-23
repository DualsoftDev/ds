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
                    let tags =
                        [
                            seg.TagPStart :> Tag; seg.TagPReset; seg.TagPEnd;
                            seg.TagAStart :> Tag; seg.TagAReset; seg.TagAEnd;
                            seg.Going; seg.Ready
                        ].Where(isNull >> not)
                    for t in tags do
                        t.Name <- $"{t.InternalName}_{q}"
                        cpuBits.Add(t) |> ignore

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

        let txrxMap = Dictionary<Tag, TagE>()
        for ch in children do
            let getTags(x:ITxRx, tx:bool) =
                match x, tx with
                    | :? SegmentBase as seg, true -> seg.TagPStart :> Tag
                    | :? SegmentBase as seg, false -> if seg.TagAEnd = null then seg.TagPEnd else seg.TagAEnd
                    | :? Tag as tag, _ -> tag
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
                    ch.TagsStart <- [ex.TagPStart :> Tag] |> ResizeArray
                    ch.TagReset <- ex.TagPReset
                    ch.TagsEnd <- [ex.TagPEnd :> Tag] |> ResizeArray

                    for t in ch.TagsStart @@ ch.TagsEnd do
                        t.Type <- t.Type ||| TagType.External
                    ch.TagReset.Type <- ch.TagReset.Type ||| TagType.External
                | _ ->
                    failwith "ERROR"

            // call 의 원래 cpu에 정의된 tag 를 현재의 cpu 에 동일하게 생성
            let allTags = ch.TagsStart @@ ch.TagsEnd @@ [ch.TagReset]
            let notYets = allTags.Where(isNull >> not).Where(fun t -> not <| txrxMap.ContainsKey(t)).ToArray()
            for t in notYets do
                txrxMap.Add(t, TagE(ch.Cpu, ch, t.Name, t.Type))
            ch.TagsStart <- ch.TagsStart.Select(fun t -> txrxMap[t]).Cast<Tag>().ToList()
            ch.TagsEnd   <- ch.TagsEnd  .Select(fun t -> txrxMap[t]).Cast<Tag>().ToList()
            if ch.TagReset <> null then
                ch.TagReset <- txrxMap[ch.TagReset]



        let activeFlowRoots =
            model.Cpus.First(fun cpu -> cpu.IsActive)
                .RootFlows.SelectMany(fun f -> f.RootSegments)
        for seg in activeFlowRoots do
            for t in [seg.TagPStart; seg.TagPReset; seg.TagPEnd] do
                t.Type <- t.Type ||| TagType.Plan ||| TagType.External
                

[<Extension>] // type Segment =
type ModelExt =
    [<Extension>]
    static member Epilogue(model:Model, opc:OpcBroker) =
        //markTxRxTags(model)
        let segments =
            model.Systems.selectMany(fun sys -> sys.RootFlows)
                .selectMany(fun rf -> rf.RootSegments)
        for segment in segments do
            segment.Epilogue()

        renameBits(model)
        markChildren(model)
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


