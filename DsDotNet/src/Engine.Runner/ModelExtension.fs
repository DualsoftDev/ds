namespace Engine.Runner

open System.Linq
open System.Collections.Generic
open System.Runtime.CompilerServices

open Engine.Common
open Engine.Common.FS
open Engine.Core
open Engine.OPC
open Engine.Graph

[<Extension>] // type Segment =
type ModelExt =
    [<Extension>]
    static member RetouchTags(model:Model, opc:OpcBroker) =

        /// rename flow/segment tags, add flow auto bit
        let renameBits() =
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

                            if p.Plan <> null then
                                cpuBits.Add(p.Plan) |> ignore
                                if p = seg.PortE then
                                    p.Plan.SetName $"{p.InternalName}_Plan_{q}"

                    cpuBits.Add f.Auto |> ignore
                    f.Auto.Name <- $"Auto_{f.QualifiedName}"

                assert(cpuBits.ForAll(cpu.BitsMap.Values.Contains))
                assert(cpuBits.OfType<Tag>().ForAll(cpu.TagsMap.Values.Contains))

        /// Tag 이름 변경으로 인한, cpu 의 BitMap/TagsMap 갱신 및 OPC tag 갱신
        let rebuildMap() =
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

        let markTxRxTags() =
            let allRootSegments =
                model.Systems.selectMany(fun s -> s.RootFlows)
                    .SelectMany(fun f -> f.RootSegments)
                

            for rs in allRootSegments do
                for tx in rs.Children.selectMany(fun ch -> ch.TagsStart).OfType<Tag>() do
                    tx.Type <- tx.Type ||| TagType.TX ||| TagType.External

                for rx in rs.Children.selectMany(fun ch -> ch.TagsEnd).OfType<Tag>() do
                    rx.Type <- rx.Type ||| TagType.RX ||| TagType.External

                for ch in rs.Children do
                    assert(ch.Coin :? ExSegmentCall)
                    let reset = ch.TagReset
                    assert(reset.Type.HasFlag(TagType.Reset))
                    reset.Type <- reset.Type ||| TagType.External

        renameBits()
        rebuildMap()
        markTxRxTags()

    [<Extension>]
    static member Epilogue(model:Model) =
        let segments =
            model.Systems.selectMany(fun sys -> sys.RootFlows)
                .selectMany(fun rf -> rf.RootSegments)
        for segment in segments do
            segment.Epilogue()


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


