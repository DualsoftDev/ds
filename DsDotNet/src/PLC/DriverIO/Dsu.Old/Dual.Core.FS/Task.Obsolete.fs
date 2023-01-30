namespace Old.Dual.Core.Ver1

open Old.Dual.Common
open System.Diagnostics

open Old.Dual.Common
open System.Runtime.CompilerServices

type IToText =
    abstract member ToText: unit -> string

/// Edge end point
type IVertex = interface end
//    abstract member ToText: unit -> string

type IEdge =
    abstract member Source: IVertex with get
    abstract member Target: IVertex with get
    abstract member Sources: seq<IVertex> with get
    abstract member Targets: seq<IVertex> with get


type ISlot = interface end


//type IEdge =
//    abstract member Source: int * IVertex with get
//    abstract member Target: int * IVertex with get

type ITask =
    interface end

type EdgeType =
    | Sync
    | Async
    /// Task 간 reset
    | Reset

    with 
        member x.ToText() = 
            match x with
            | Sync -> "─>"
            | Async -> "-->"
            | Reset -> "==>"

type TaskStatus =
    | Run
    | Standby
    | Finish


type AddrType = 
    | I
    | Q
    | M
    | Phys

    with
        member x.ToText() =
            match x with
            | I -> "I_"
            | Q -> "Q_"
            | M -> "M_"
            | Phys -> "Phys_"

/// Conditional Operator : OR, ...
type Condional() =
    interface IVertex 
    member x.ToText() = ""

type Slot(name, addrType, task) =
    new(name, addrType) = Slot(name, addrType, None)
    new(task) = Slot(null, AddrType.M, task)

    interface IVertex 

    member val ParentTask:ITask option = task with get, set
    member val Connections:List<IVertex> = List.empty
    member val Name = name with get, set
    member val AddrType = addrType with get, set
    member x.ToText() = addrType.ToText() + x.Name

type Button = Slot


/// Task 로 들어오는 Action
type In(name, addrType, task) =
    inherit Slot(name, addrType, task)
    new(name, addrType) = In(name, addrType, None)
    new(task) = In(null, AddrType.M, task)

/// Task 에서 나가는 State
type Out(name, addrType, task) =
    inherit Slot(name, addrType, task)
    new(name, addrType) = Out(name, addrType, None)
    new(task) = Out(null, AddrType.M, task)

    
type Edge(source, target, edgeType : EdgeType) =
    let mutable source:IVertex = source
    let mutable target:IVertex = target

    new(source, target) = Edge(source, target, EdgeType.Sync)
    interface IEdge with
        member x.Source with get() = source
        member x.Target with get() = target
        member x.Sources with get() = seq {source}
        member x.Targets with get() = seq {target}
    member x.Source with get() = source and set(v) = source <- v
    member x.Target with get() = target and set(v) = target <- v
    member val Type = edgeType with get, set

    //member x.ToText() = source.ToText() + " " + x.Type.ToText() + " " + target.ToText()

/// Task 정의
[<DebuggerDisplay("{ToText()}")>]
type Task(parent, name, ins:seq<In>, outs:seq<Out>) as this =
    let mutable ins = ins |> ResizeArray.ofSeq
    let mutable outs = outs |> ResizeArray.ofSeq
    let itask = Some(this :> ITask)
    let mutable parent:Task option = parent

    do
        ins |> Seq.iter (fun v -> v.ParentTask <- itask)
        outs |> Seq.iter (fun v -> v.ParentTask <- itask)

    interface ITask

    /// Task 정의
    new(name) = Task(None, name, Seq.empty, Seq.empty)
    /// Task 정의
    new() = Task("")


    member x.ParentTask with get() = parent and set(v) = parent <- v
    member val Reset = In("Reset", AddrType.M, itask) with get, set
    member val Edges : ResizeArray<IEdge> = ResizeArray() with get, set
    
    member val Name = name with get, set

    member x.AsITask() = this :> ITask
    member x.Ins 
        with get() = ins
        and set(v:ResizeArray<In>) = 
            ins <- v
            v |> Seq.iter(fun i -> i.ParentTask <- itask)

    member x.Outs 
        with get() = outs 
        and set(v:ResizeArray<Out>) = 
            outs <- v
            v |> Seq.iter(fun i -> i.ParentTask <- itask)

    // 추가 구현 사항 들 ...
    // member x.SubTasks:List<Task> with get, set
    //member x.DAGs with get() = 
    //    let rec findnext (idx) : seq<seq<Edge>> =
    //        let tedges = x.Edges |> Seq.where(fun e -> e.Type <> EdgeType.Reset) |> Seq.where(fun e -> fst e.Source = idx) |> Seq.groupBy(fun e -> e.Type)

    //        let sync = tedges |> Seq.where(fun e -> fst e = EdgeType.Sync) |> Seq.map(fun t -> snd t) |> Seq.flatten
    //        let async = tedges |> Seq.where(fun e -> fst e = EdgeType.Async) |> Seq.map(fun t -> snd t) |> Seq.flatten

    //        let syncnext = sync |> Seq.collect(fun e -> findnext (fst e.Target) )
    //        let ns = syncnext |> Seq.where(fun s -> (s |> Seq.head).Type = EdgeType.Sync) |> Seq.flatten
    //        let na = syncnext |> Seq.where(fun s -> (s |> Seq.head).Type <> EdgeType.Sync)

    //        seq{
    //            sync |> Seq.append ns
    //        }
    //        |> Seq.append na
    //        |> Seq.append (async |> Seq.collect(fun e -> findnext (fst e.Target) ))
            
    
    //    findnext 0

    // status : Task 의 동적 상태
    member val Status = TaskStatus.Standby

    member x.ToText() =
        //let getSlot (v:IVertex) =
        //    match v with
        //    | :? IndexedSlot as s -> s.Slot
        //    | :? Slot as s -> s
        //    | _ -> failwith "ERROR"

        //let getEdgeTexts (t:Task) =
        //    t.Edges
        //    |> Seq.map(fun e -> e.ToText() + "\n")
        //    |> Seq.tryReduce(fun a b -> a + b)
        //    |> Option.defaultValue ""
        //let children = 
        //    x.Edges
        //        |> Seq.collect (fun e -> [e.Source; e.Target])
        //        |> Seq.map (fun v -> getSlot(v).ParentTask)
        //        |> Seq.choose id
        //        |> Seq.distinct

        //children
        //|> Seq.map(fun t -> getEdgeTexts (t :?> Task) + "\n" )
        //|> Seq.tryReduce (+)
        //|> Option.defaultValue ""
        ""

    //member x.ToText() = (fst x.Source).ToString() + (snd x.Source).ToText() + " " + x.Type.ToText() + " " + (fst x.Target).ToString() + (snd x.Target).ToText()


/// Device Task
type TerminalTask() =
    inherit Task()

/// System Task
type InitialTask() =
    inherit Task()


type Circle(t:Task) =
    member x.Task = t
    interface IVertex

type CircleEdgeTip(circle:Circle, slot:Slot) =
    member x.Circle = circle
    member x.Slot = slot
    interface IVertex

/// 모델링 단에서 사용하는 circle(job) 간의 연결 edge
type CircleEdge(src:CircleEdgeTip, tgt:CircleEdgeTip) =
    member x.Source = src
    member x.Target = tgt
    interface IEdge with
        member x.Source with get() = x.Source.Circle :> IVertex
        member x.Target with get() = x.Target :> IVertex
        member x.Sources with get() = seq {x.Source :> IVertex; x.Source.Slot :> IVertex}
        member x.Targets with get() = seq {x.Target :> IVertex; x.Target.Slot :> IVertex}



[<Extension>] // type GraphvizExt =
type TaskGraphExt =
    [<Extension>] static member WithName(outs:Out seq, name) = outs |> Seq.tryFind(fun o -> o.Name = name)
    [<Extension>] static member WithName(ins:In seq, name) = ins |> Seq.tryFind(fun o -> o.Name = name)

