namespace Old.Dual.Core

open System.Runtime.CompilerServices
open QuickGraph
open FSharpPlus
open Old.Dual.Common
open Old.Dual.Common.Graph
open Old.Dual.Common.Graph.QuickGraph

[<AutoOpen>]
module DeviceModule =
    [<Extension>] // type DeviceExt =
    type DeviceExt =
        [<Extension>]
        static member GetLeftSlots(dev:Device) =
            dev.Tasks |> Seq.collect(fun t -> t.GetLeftSlots())
        [<Extension>]
        static member GetRightSlots(dev:Device) =
            dev.Tasks |> Seq.collect(fun t -> t.GetRightSlots())
        [<Extension>]
        static member GetSlots(dev:Device) =
            (dev.GetLeftSlots() |> Seq.cast<Slot>) @@ (dev.GetRightSlots() |> Seq.cast<Slot>)

        [<Extension>]
        static member CreateTaskResetGraph(dev:Device, edges:seq<DeviceTaskResetEdge>) =
            dev.TaskResetGraph <- Some (DeviceTaskResetGraph(edges))

    [<Extension>] // type DeviceTaskResetGraphExt =
    type DeviceTaskResetGraphExt =
        /// cause task 에 의해서 reset 당하는 tasks 반환
        [<Extension>]
        static member GetResetedTasks(g:DeviceTaskResetGraph, cause:DeviceTask) =
            getOutgoingNodes g.Graph cause
        [<Extension>]
        static member GetResetingTasks(g:DeviceTaskResetGraph, effect:DeviceTask) =
            g.Graph.GetIncomingVertices effect
