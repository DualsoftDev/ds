namespace Engine.Runner

open System.Linq
open System.Collections.Generic
open System.Runtime.CompilerServices

open Engine.Common.FS
open Engine.Core
open Engine.OPC
open Engine.Graph

[<AutoOpen>]
module internal ModelModule =
        /// rename flow/segment tags, add flow auto bit
    let renameBits(model:Model) =
        for f in model.Cpus.selectMany(fun f -> f.RootFlows) do
            f.Auto.Name <- $"Auto_{f.QualifiedName}"

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
                    | :? Segment as seg, true -> seg.TagPStart :> Tag
                    | :? Segment as seg, false -> if seg.TagAEnd = null then seg.TagPEnd else seg.TagAEnd
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
                    let ex = exSegCall.ExternalSegment :?> Segment
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
                .RootFlows.SelectMany(fun f -> f.RootSegments).Cast<Segment>()
        for seg in activeFlowRoots do
            for t in [seg.TagPStart; seg.TagPReset; seg.TagPEnd] do
                t.Type <- t.Type ||| TagType.Plan ||| TagType.External
                
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

    let checkCpu(model:Model) =
        let check(cpu:Cpu) =
            let exprs = cpu.BitsMap.Values.OfType<BitReEvaluatable>().ToArray()
            for ex in exprs do
                assert(ex.Evaluate() = ex.Value)
        for cpu in model.Cpus do
            check cpu

[<AutoOpen>]
module internal ModelPrintModule =
    let bitToString(bit:IBit) =
        if isNull bit then
            "-"
        else if bit.Value then "1"
        else "0"
    let portInfoToString (pi:PortInfo) =
        $"{bitToString pi.Plan}:{bitToString pi.Actual}"
        
        
    let printSegment(segment:SegmentBase) =
        let seg = segment :?> Segment
        let p = bitToString
        let pp = portInfoToString

        logDebug $"{seg.QualifiedName} {seg.Status}"
        logDebug $"\tTagPlan(S/R/E)  =({p seg.TagPStart}/{p seg.TagPReset}/{p seg.TagPEnd})"
        logDebug $"\tTagActual(S/R/E)=({p seg.TagAStart}/{p seg.TagAReset}/{p seg.TagAEnd})"
        logDebug $"\tPortInfo(Plan:Actual) =({pp seg.PortS}/{pp seg.PortR}/{pp seg.PortE})"

    let getRootSegments (model:Model) =
        model.Systems.selectMany(fun sys -> sys.RootFlows)
            .selectMany(fun rf -> rf.RootSegments)

    let printModel (model:Model) =
        logDebug ":::::::: Root Segments"
        for segment in getRootSegments model do
            printSegment segment

        if Global.Model.VPSs <> null then
            logDebug ":::::::: Virtual Parent Segments"
            for vps in Global.Model.VPSs do
                printSegment vps

[<Extension>] // type Segment =
type ModelExt =
    [<Extension>]
    static member Epilogue(model:Model, opc:OpcBroker) =
        for segment in getRootSegments model do
            segment.Epilogue()

        renameBits(model)
        markChildren(model)
        rebuildMap(model, opc)
        checkCpu(model)

    [<Extension>]
    static member Print(model:Model) = printModel model

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


