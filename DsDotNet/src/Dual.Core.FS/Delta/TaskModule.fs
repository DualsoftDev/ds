namespace Dual.Core

open System.Runtime.CompilerServices
open QuickGraph
open FSharpPlus
open Dual.Common


[<AutoOpen>]
module TaskModule =
    let private setEdgesSlotOwner (containerTask:Task, edges:seq<Edge>) =
        let endPoints = edges |> Seq.collect(fun e -> [e.Source; e.Target])
        for v in endPoints do
            match v with
            | :? CircleSlot as cslot -> ()  // cslot.Slot.ContainerTask <- containerTask
            | :? Slot as slot -> slot.ContainerTask <- containerTask
            | _ -> failwith "Unknown"

    let private defaultTaskCreator(name, edges) = Task(name, edges)
    let private createTask(name:string, edges, taskCreator) =
        let task = taskCreator(name, edges)
        setEdgesSlotOwner(task, edges)
        task
    type TaskFactory =
        static member Create(name, edges:seq<Edge>) = createTask(name, edges, defaultTaskCreator)
        static member Create(name) = TaskFactory.Create(name, Seq.empty)
        static member CreateDeviceTask(name, edges:seq<Edge>)  = createTask(name, edges, fun (n, es) -> DeviceTask(n, es))
        static member CreateDeviceTask(name, edge:Edge)        = createTask(name, [edge], fun (n, es) -> DeviceTask(n, es))
        static member CreateDeviceTask(name)                   = TaskFactory.CreateDeviceTask(name, Seq.empty)
        static member CreateControlTask(name, edges:seq<Edge>) = createTask(name, edges, fun (n, es) -> ControlTask(n, es))
        static member CreateControlTask(name)                  = TaskFactory.CreateControlTask(name, Seq.empty)


    [<Extension>] // type TaskFactoryExt =
    type TaskFactoryExt =
        [<Extension>]
        static member AddEdges(task:Task, edges:seq<Edge>) =
            setEdgesSlotOwner(task, edges)
            task.AddEdgesInternal(edges)

        [<Extension>]
        static member GetLeftSlots(task:Task) =
            let isolated = task.IsolatedSlots |> Seq.ofType<LeftSlot>
            let connected =
                task.Edges 
                |> List.map(fun e -> e.Source)
                |> List.ofType<LeftSlot>
            isolated @@ connected
            |> Seq.distinct
            |> List.ofSeq

        [<Extension>]
        static member GetRightSlots(task:Task) =
            let isolated = task.IsolatedSlots |> Seq.ofType<RightSlot>
            let connected =
                task.Edges
                |> List.map(fun e -> e.Target)
                |> List.ofType<RightSlot>
            isolated @@ connected
            |> Seq.distinct
            |> List.ofSeq

        [<Extension>]
        static member GetSlots(task:Task) =
            (task.GetLeftSlots() |> List.cast<Slot>) @ (task.GetRightSlots() |> List.cast<Slot>)

        [<Extension>]
        static member GetNamedSlot(task:Task, name) =
            task.GetSlots() |> List.tryFind(fun s -> s.Name = name)

        [<Extension>]
        static member AddIsolatedSlot(task:Task, slot:Slot) = task.IsolatedSlots.Add(slot)
        [<Extension>]
        static member AddIsolatedSlots(task:Task, slots:seq<Slot>) = task.IsolatedSlots.AddRange(slots)

        /// Task 하부에 모델링에 사용된 circle 을 반환
        [<Extension>]
        static member GetSubCircles(task:Task) =
            task.Edges
            |> List.collect(fun e -> [e.SourceCircle; e.TargetCircle])
            |> List.choose id
            |> List.distinct
            |> List.cast<Circle>

        /// Task 에 포함된 하부 tasks 를 반환
        [<Extension>]
        static member GetSubTasks(task:Task) = task.GetSubCircles() |> List.map(fun circle -> circle.Task)

