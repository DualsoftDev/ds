// Learn more about F# at http://fsharp.org
open Engine.Core
open System.Linq
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
        let edgeExVpPp = flowEx.CreateEdges(segExVp, segExPp, ">")
        let edgeExPpSp = flowEx.CreateEdges(segExPp, segExSp, ">")
        let segExVm = Segment.Create("Vm", flowEx)
        let segExPm = Segment.Create("Pm", flowEx)
        let segExSm = Segment.Create("Sm", flowEx)
        let edgeExVmPm = flowEx.CreateEdges(segExVm, segExPm, ">")
        let edgeExPmSm = flowEx.CreateEdges(segExPm, segExSm, ">")

        let edgeExResetVpVm = flowEx.CreateEdges(segExVp, segExVm, "||>")
        let edgeExResetVmVp = flowEx.CreateEdges(segExVm, segExVp, "||>")

        flowEx.Graph.AddEdges([edgeExVpPp; edgeExPpSp; edgeExVmPm; edgeExPmSm] |> Seq.collect id) |> verify "Duplicated!"
        flowEx.Graph.AddEdges([edgeExResetVpVm; edgeExResetVmVp] |> Seq.collect id) |> verify "Duplicated!"

        let system = DsSystem.Create("my", cpu, model)
        let flow = Flow.Create("F", system)
        let seg = Segment.Create("R1", flow)
        let cp = ApiItem.Create("\"C+\"", system)
        cp.TXs.Add(segExVp)  |> verify "Duplicated!"
        cp.RXs.Add(segExSp)  |> verify "Duplicated!"

        let cm = ApiItem.Create("\"C-\"", system)
        cm.TXs.Add(segExVm)  |> verify "Duplicated!"
        cm.RXs.Add(segExSm)  |> verify "Duplicated!"

        let childCp = ChildApiCall.Create(cp, seg)
        let childCm = ChildApiCall.Create(cm, seg)
        let childEdgeCpCm = seg.CreateEdges(childCp, childCm, ">")
        seg.Graph.AddEdges(childEdgeCpCm) |> verify "Duplicated!"

        let childCp2 = ChildAliased.Create("\"C+2\"", cp, seg)
        let childCm2 = ChildAliased.Create("\"C-2\"", cm, seg)
        let childEdgeCpCm2 = seg.CreateEdges(childCp2, childCm2, ">")
        seg.Graph.AddEdges(childEdgeCpCm2) |> verify "Duplicated!"


        let key1 = [|"seg1"|]
        let values1 = HashSet<string>([|"seg2"|]);
        flow.AliasMap.Add(key1, values1)
        flow.AliasMap[key1].Add("seg3") |> verify "Duplicated!"
        //flow.AliasMap[key1].Add("seg3") |> verify "Duplicated!  should fail"
        model

[<EntryPoint>]
let main argv =
    printfn "Hello World from F#!"
    let model = Exercise.createSample()
    let xs = model.Spit().ToArray()
    0 // return an integer exit code