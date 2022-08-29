namespace Dual.Core

open System.Diagnostics
open Dual.Common
open Dual.Core.Types


type ITask = interface end

/// Graph 상의 circle : Circle, CircleSlot
type ICircle =
    inherit IVertex
    abstract member Circle:ICircle with get


type AddrType = I | Q | M | Phys

/// Edge 의 끝단에 해당하는 객체의 interface
/// Slot, CircleSlot
type ISlot =
    inherit IVertex
    abstract member Slot:Slot with get
and
    [<DebuggerDisplay("{ToText()}")>]
    Slot(name, addrType) as this =
        let mutable name = name
        let mutable containerTask:ITask option = None
        interface ISlot with
            member x.Slot with get() = this
        interface IVertex with
            member x.Name = name
            member x.Properties = empty |> dict 
            member x.Parent     with get() = None and set(v) = ()
            member x.Reset      = Expression.Zero
            member x.Ports with get() = empty |> dict and set(v) = ()
            member x.DAGs       with get() = empty |> ResizeArray and set(v) = ()
            member x.ToText() = x.ToText()

        member x.GetIncomingEdges():seq<Edge> = Seq.empty
        member x.GetOutgoingEdges():seq<Edge> = Seq.empty
        member x.ContainerTask with get() = containerTask.Value and set(v) = containerTask <- Some(v)

        member x.Name with get() = name and set(v) = name <- v
        member val AddrType:AddrType = addrType with get
        member x.ToText() = sprintf "%s(%A)" x.Name addrType

/// ISlot(--> Slot, CircleSlot) 간의 edge
and 
    [<DebuggerDisplay("{ToText()}")>]
    Edge(source, target, edgeType) =
        let mutable source:ISlot = source
        let mutable target:ISlot = target
        new (source, target) = Edge(source, target, EdgeType.Sync)
        member val EdgeType = edgeType with get, set
        //member x.Slots = [source.Slot; target.Slot]
        member x.Source = source
        member x.Target = target
        member x.SourceSlot = source.Slot
        member x.TargetSlot = target.Slot
        member x.SourceCircle:ICircle option =
            match source with
            | :? ICircle as circle -> Some circle.Circle
            | _ -> None
        member x.TargetCircle:ICircle option =
            match target with
            | :? ICircle as circle -> Some circle.Circle
            | _ -> None
        member x.ToText() =
            sprintf "Source: %s\r\nTarget:%s" (source.ToText()) (target.ToText())
        interface Dual.Core.Prelude.IText with
            member x.ToText() = x.ToText()
        interface IEdge with
            member x.Source = source :> IVertex
            member x.Target = target :> IVertex
            member x.EdgeType = x.EdgeType

/// Task 로 들어오는 Action
type LeftSlot(name, addrType) =
    inherit Slot(name, addrType)

/// Task 에서 나가는 State
type RightSlot(name, addrType) =
    inherit Slot(name, addrType)

type In = LeftSlot
type Out = RightSlot

/// Task 클래스.  see TaskModule.fs
[<DebuggerDisplay("{ToText()}")>]
type Task internal (name, edges:seq<Edge>) =
    let mutable edges = edges |> List.ofSeq

    // edge 에 포함되지 않은 / 아직 연결되지 않은 slot
    let isolatedSlots: ResizeArray<Slot> = ResizeArray()

    internal new (name) = Task(name, Seq.empty)
    member val Name:string = name with get, set
    /// this Task 내부에 포함된 모든 edge.  내/외부 slot 과 circle 간의 모든 edge
    member x.Edges with get() = edges
    member internal x.AddEdgesInternal(es:seq<Edge>) = edges <- edges @ (es |> List.ofSeq)

    /// edge 에 포함되지 않은 / 아직 연결되지 않은 slot
    member internal x.IsolatedSlots = isolatedSlots

    member x.ToText() =
        sprintf "%s" name
    interface ITask


type ControlTask internal (name, edges:seq<Edge>) =
    inherit Task(name, edges)

type Named(name) =
    let mutable name = name
    member x.Name with get() = name and set(v) = name <- v
    interface INamed with
        member x.Name = name

/// Task 를 모델링에서 사용할 때, 동그라미로 표현되는 job 형태.
/// 실제 하나의 Task 객체에 대한 다중의 reference 를 표현하기 위한 class
/// see CircleModule.fs
[<DebuggerDisplay("{ToText()}")>]
type Circle(name, task) as this =
    inherit Named(name)
    /// 실제의 Task instance
    member x.Task:Task = task
    /// Circle 의 name.  참조하는 task 의 name 과 다를 수도 있다.  e.g task.Name = "A", circle.Name = "A+"
    /// Task 는 device 전체에 해당하고, Circle 은 device 의 job 에 해당하는 개념
    member x.ToText() =
        sprintf "Task = %s, Circle = %s" (task.ToText()) name

    interface ICircle with
        member x.Circle = this :> ICircle
        member x.Name = x.Name
        member x.ToText() = x.ToText()
        member x.Reset      = Expression.Zero
        // IVertex interface
        member x.Parent with get() = None and set(v) = ()
        member x.Properties = empty |> dict 
        //member x.Reset = Expression.Zero
        member x.Ports with get() = empty |> dict and set(v) = ()
        member x.DAGs       with get() = empty |> ResizeArray and set(v) = ()

/// 모델링 단계에서 Circle 에 부착되는 slot.
/// Circle 에 대한 참조 및 Circle 에 포함된 Task 내부의 slot 에 대한 참조
type CircleSlot(circle, slot) =
    member x.Slot:Slot = slot
    member x.Circle:Circle = circle
    interface ISlot with
        member x.Slot = x.Slot
    interface ICircle with
        member x.Circle = x.Circle :> ICircle
    interface IVertex with
        member x.Reset      = Expression.Zero
        member x.Parent with get() = None and set(v) = ()
        member x.Properties = empty |> dict 
        //member x.Reset = Expression.Zero
        member x.Ports with get() = empty |> dict and set(v) = ()
        member x.DAGs        with get() = empty |> ResizeArray and set(v) = ()
        member x.ToText() = x.ToText()
    interface INamed with
        member x.Name with get() = failwith "Not yet implemented"

    member x.ToText() = sprintf "Slot:%s, Circle:%s" (slot.ToText()) (circle.ToText())

