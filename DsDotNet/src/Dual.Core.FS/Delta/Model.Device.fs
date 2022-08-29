namespace Dual.Core

open Dual.Common
open QuickGraph
open FSharpPlus

/// 하나의 device 를 표현하는 task.  e.g : Cylinder device task (--> Adv, Rtn 을 포함)
/// see TaskFactory.CreateDeviceTask
type DeviceTask internal (name, edges:seq<Edge>, device) =
    inherit Task(name, edges)
    let mutable device:Device option = device

    new (name, edges) = DeviceTask(name, edges, None)
    member x.Device with get() = device and set(v) = device <- v

/// device task 간의 reset 관계 edge.  src task 수행시 dst task 가 reset 됨을 의미
and DeviceTaskResetEdge(src, dst) =
    inherit Edge<DeviceTask>(src, dst)

/// device task 간의 reset 관계 graph
and DeviceTaskResetGraph(edges:seq<DeviceTaskResetEdge>) =
    /// AdjacencyGraph<DeviceTask, DeviceTaskResetEdge>
    let g = edges.ToAdjacencyGraph()
    member x.Graph = g


/// 물리 device 하나를 표현하는 class (e.g cylinder)
/// devTasks : device 에 포함된 task (e.g 전진, 후진)
/// resetGraph : task 간의 reset 관계를 표현한 graph (e.g 전진 -> 후진 edge 는 전진 수행 시, 후진이 reset 됨)
and Device(name, devTasks:seq<DeviceTask>, resetGraph) as this =
    inherit Named(name)
    let deviceTasks = devTasks |> ResizeArray.ofSeq
    let mutable resetGraph:DeviceTaskResetGraph option = resetGraph
    do
        deviceTasks |> Seq.iter(fun t -> t.Device <- Some this)

    new (name, devTasks) = Device(name, devTasks, None)
    new (name, devTasks, resetEdges:seq<DeviceTaskResetEdge>) =
        let g = DeviceTaskResetGraph(resetEdges)
        Device(name, devTasks, Some g)
    member x.Tasks = deviceTasks
    member x.TaskResetGraph with get() = resetGraph and set(v) = resetGraph <- v

