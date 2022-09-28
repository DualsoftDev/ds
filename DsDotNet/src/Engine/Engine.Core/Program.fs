// Learn more about F# at http://fsharp.org
open Engine.Core
open System
open Engine.Core.CoreModule
open System.Collections.Generic

module Exercise =
    let createSample() =
        let model = Model()
        let cpu = { new ICpu }
        let systemEx = DsSystem.Create("EX", cpu, model)
        let flowEx = Flow.Create("F", systemEx)
        let segExVp = Segment.Create("Vp", flowEx)
        let segExPp = Segment.Create("Pp", flowEx)
        let segExSp = Segment.Create("Sp", flowEx)
        let edgeExVpPp = InFlowEdge(segExVp, segExPp, EdgeType.Default)
        let edgeExPpSp = InFlowEdge(segExPp, segExSp, EdgeType.Default)
        let segExVm = Segment.Create("Vm", flowEx)
        let segExPm = Segment.Create("Pm", flowEx)
        let segExSm = Segment.Create("Sm", flowEx)
        let edgeExVmPm = InFlowEdge(segExVm, segExPm, EdgeType.Default)
        let edgeExPmSm = InFlowEdge(segExPm, segExSm, EdgeType.Default)

        let edgeExResetVpVm = InFlowEdge(segExVp, segExVm, EdgeType.Reset ||| EdgeType.Strong)
        let edgeExResetVmVp = InFlowEdge(segExVm, segExVp, EdgeType.Reset ||| EdgeType.Strong)

        flowEx.AddEdges([edgeExVpPp; edgeExPpSp; edgeExVmPm; edgeExPmSm]) |> verify "Duplicated!"
        flowEx.AddEdges([edgeExResetVpVm; edgeExResetVmVp]) |> verify "Duplicated!"

        let system = DsSystem.Create("my", cpu, model)
        let flow = Flow.Create("F", system)
        let seg = Segment.Create("R1", flow)
        let cp = InterfacePrototype.Create("\"C+\"", system)
        cp.TXs.Add(segExVp)  |> verify "Duplicated!"
        cp.RXs.Add(segExSp)  |> verify "Duplicated!"

        let cm = InterfacePrototype.Create("\"C-\"", system)
        cm.TXs.Add(segExVm)  |> verify "Duplicated!"
        cm.RXs.Add(segExSm)  |> verify "Duplicated!"

        let childCp = Child.Create("\"C+\"", cp, seg)
        let childCm = Child.Create("\"C-\"", cm, seg)
        let childEdgeCpCm = InSegmentEdge(childCp, childCm, EdgeType.Default)
        seg.AddEdge(childEdgeCpCm) |> verify "Duplicated!"

        let key1 = [|"my"; "flow"; "seg1"|]
        let values1 = HashSet<string>([|"seg2"|]);
        flow.AliasMap.Add(key1, values1)
        flow.AliasMap[key1].Add("seg3") |> verify "Duplicated!"
        //flow.AliasMap[key1].Add("seg3") |> verify "Duplicated!  should fail"
        model

[<EntryPoint>]
let main argv =
    printfn "Hello World from F#!"
    let model = Exercise.createSample()
    0 // return an integer exit code