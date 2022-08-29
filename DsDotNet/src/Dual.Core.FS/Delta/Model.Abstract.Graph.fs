namespace Dual.Core

open System.Diagnostics
open Dual.Common
open Dual.Core.Types
open System.Collections.Generic
open Dual.Core.Types.Command
open Dual.Core.Prelude


type VertexPropertyType =
    | Address
    | InitialStatus
    | UseSelfHold
    | UseOutputInterlock
    | UseOutputResetByWorkFinish
    | ManualTag
    

[<AutoOpen>]
module ModelAbstractGraph =
    type INamed = 
        abstract member Name:string with get

    /// reset interface (read only)
    type IReset =
        /// Memory reset 조건.  vertex 가 A+ 라면 A- 시작 시에 reset 된다.  NodeStartCondtion(A-)
        abstract member Reset:Expression with get


    /// DAG 그래프 상의 branch 의 type
    type BranchType =
        /// 동시 조건.  task 로 들어오는/나가는 조건이 동시
        | And
        /// XOR 조건.  task 로 들어오는/나가는 조건이 XOR
        | Xor

    /// Vertex status
    type VertexStatus =
        | Ready
        | Finish
        | Undefined
        | Impossible

    type PortCategory =
        | Start
        | Sensor
        | Ready
        | Going
        | Finish
        | Homing
        | Reset

    type EdgeType =
        | Sync
        | Async
        | Global
        /// Task 간 reset
        | Reset

    type SegmentType =
        | Internal
        | External

    type NegPLCTag(tag, ioType, address:Address option, properties:(string*_) array) =
        inherit PLCTag(tag, ioType, address, properties)

        new (t, iot, a)         = NegPLCTag(t, iot, a, [||])

        override x.Equals t = 
            match t with
            | :? NegPLCTag as pt -> 
                x.ToText() = pt.ToText()
                && x.IOType = pt.IOType
                && x.Address = pt.Address
            | _ -> false

    /// Graph 상의 vertex 에 해당할 수 있는 객체의 interface.
    /// ISlot, Slot, ICircle, Circle, CircleSlot
    type IVertex =
        inherit INamed
        inherit IReset
        inherit IText

        abstract member Parent: IVertex option with get, set
        abstract member Ports : IDictionary<PortCategory, IPort> with get, set
        /// Properties
        abstract member Properties: IDictionary<VertexPropertyType, obj> with get
        // 하위 DAG
        abstract member DAGs: IDAG ResizeArray with get, set

    /// Port
    /// IVertex 에 소속 Read/Write port
    and IPort =
        /// IPort가 소속된 버텍스
        abstract member Parent:IVertex with get, set
        abstract member PortType:PortCategory with get

        abstract member ConnectedPorts:IPort ResizeArray with get
        abstract member ConditionExpression:Expression with get, set
        abstract member InterlockExpression:Expression with get, set
        abstract member PLCFunctions:IFunctionCommand ResizeArray with get, set

        abstract member TagType:TagType option with get, set
        abstract member Address:(Address * bool) ResizeArray with get, set

        /// 핵심 Tag..센서,코일,메모리
        abstract member PLCTags:PLCTag ResizeArray with get, set
        /// PLCTag와 Function 연결을 일반화 시키기 위한 Dummy Tag
        /// Port가 Start, Reset일 경우 function을 실행시키는 tag
        /// 이외 port의 경우 function 결과 tag이다.
        abstract member DummyTag:PLCTag option with get, set
        /// sensor가 여러개 일 경우 하나의 단 메모리를 생성한다.
        abstract member EndTag:PLCTag option with get, set
        

    and IUserPort =
        inherit IPort
        /// 내부에서 연결된 Port들
        abstract member InnerConnectedPorts:IPort ResizeArray with get

    /// Edge 의 interface
    and IEdge = 
        inherit QuickGraph.IEdge<IVertex>
        abstract member EdgeType: EdgeType with get

    /// DAG(Directed acyclic graph) 를 표현하는 interface
    and IDAG =
        abstract member Vertices:IVertex seq with get
        abstract member Edges:IEdge seq with get
            

    /// src, tgt 두개의 vertex 를 연결하는 edge 생성자
    type EdgeCreator = IVertex -> IVertex -> IEdge

    type ISystem =
        abstract member Vertices:IVertex ResizeArray with get
        abstract member Edges:IEdge ResizeArray with get

    type ISegment =
        abstract member SegmentType: SegmentType with get, set
        abstract member Vertices:IVertex ResizeArray with get

    type ISelect =
        inherit IVertex

        abstract member ConditionDAG: Dictionary<IFunctionCommand list, IDAG> with get
        abstract member AddConditionDAG:IFunctionCommand list * IDAG -> unit

    

[<AutoOpen>]
module IVertexExtension =
    let getPort (v:IVertex) (portCate:PortCategory) = 
        if v.Ports.ContainsKey(portCate) then v.Ports.[portCate] else failwithlogf "%A의 %s가 존재하지 않습니다." v (portCate.ToString())
    let setPort (v:IVertex) (portCate:PortCategory) value = 
        if v.Ports.ContainsKey(portCate) then v.Ports.[portCate] <- value else v.Ports.Add(portCate, value) 
    let getProperty<'T> (key:VertexPropertyType) (v:IVertex) =
        if v.Properties.ContainsKey(key) then
            v.Properties.[key] :?> 'T |> Some
        else 
            None
    let setProperty (key:VertexPropertyType) (value:obj) (v:IVertex) =
        if v.Properties.ContainsKey(key) then
            v.Properties.[key] <- value
        else
            v.Properties.Add(key, value)

    type IVertex with
        /// Vertex의 초기 상태
        /// DefaultValue : VertexStatus.Undefined
        member x.InitialStatus 
            with get() = getProperty<VertexStatus> VertexPropertyType.InitialStatus x |> Option.defaultValue VertexStatus.Undefined
            and set(v:VertexStatus) = setProperty VertexPropertyType.InitialStatus v x

        /// Vertex의 Dag 상 순서
        /// DefaultValue : 0
        member x.Address 
            with get() = getProperty<int> VertexPropertyType.Address x |> Option.defaultValue 0
            and set(v:int) = setProperty VertexPropertyType.Address v x

        /// 출력 자기 유지 적용 여부
        /// DefaultValue : false
        member x.UseSelfHold
            with get() = getProperty<bool> VertexPropertyType.UseSelfHold x |> Option.defaultValue false
            and set(v:bool) = setProperty VertexPropertyType.UseSelfHold v x

        /// 리셋 interlock 사용 여부.  A+ 출력 조건에 A- 출력 비접 추가
        /// DefaultValue : true
        member x.UseOutputInterlock
            with get() = getProperty<bool> VertexPropertyType.UseOutputInterlock x |> Option.defaultValue true
            and set(v:bool) = setProperty VertexPropertyType.UseOutputInterlock v x

        /// 출력의 reset (출력 완료 비접) 사용 여부.  A+ 출력 조건에 A+ 센서 비접 추가.  Physical model debugging 용
        /// DefaultValue : true
        member x.UseOutputResetByWorkFinish
            with get() = getProperty<bool> VertexPropertyType.UseOutputResetByWorkFinish x |> Option.defaultValue true
            and set(v:bool) = setProperty VertexPropertyType.UseOutputResetByWorkFinish v x

        /// Vertex 메뉴얼 조건 태그 
        /// DefaultValue : None
        member x.ManualTag
            with get() = getProperty<PLCTag> VertexPropertyType.ManualTag x 
            and set(v:PLCTag) = setProperty VertexPropertyType.ManualTag v x


        member x.StartPort
            with get() = getPort x PortCategory.Start
            and set(v) = setPort x PortCategory.Start v

        member x.SensorPort
            with get() = getPort x PortCategory.Sensor
            and set(v) = setPort x PortCategory.Sensor v

        member x.ResetPort
            with get() = getPort x PortCategory.Reset
            and set(v) = setPort x PortCategory.Reset v

        member x.GoingPort
            with get() = getPort x PortCategory.Going
            and set(v) = setPort x PortCategory.Going v

        member x.HomingPort
            with get() = getPort x PortCategory.Homing
            and set(v) = setPort x PortCategory.Homing v

        member x.ReadyPort
            with get() = getPort x PortCategory.Ready
            and set(v) = setPort x PortCategory.Ready v

        member x.FinishPort
            with get() = getPort x PortCategory.Finish
            and set(v) = setPort x PortCategory.Finish v

[<AutoOpen>]
module IPortExtension =
    type IPort with
        member x.ConnectedVertices = x.ConnectedPorts |> Seq.map(fun p -> p.Parent) |> Seq.distinct
        member x.GetTag() =
            match x.PLCTags.length() > 1 with
            | true -> [x.EndTag] |> Seq.choose id |> List.ofSeq
            | false -> x.PLCTags |> List.ofSeq
                
        /// 다른 port에서 조건으로써 사용되는 Tag
        member x.GetTerminal() = 
            let dan = 
                match x.PLCTags.length() > 1 with
                | true -> x.EndTag
                | false -> x.PLCTags |> Seq.tryHead
            let tag = 
                if x.PLCFunctions |> Seq.any then 
                    if x.PortType = PortCategory.Start || x.PortType = PortCategory.Reset then
                        dan
                    else 
                        x.DummyTag
                else 
                    dan

            match tag with
            | Some(t) -> 
                match t with
                | :? NegPLCTag as nt -> nt |> mkTerminal |> mkNeg
                | _ as t -> t |> mkTerminal
            | None -> failwithlogf "%A의 태그가 존재하지않습니다." x

        /// 다른 port에서 target으로써 사용되는 Tag Terminal
        member x.GetCoil() =
            let coils = 
                if x.PLCFunctions |> Seq.any then 
                    if  x.PortType = PortCategory.Start || x.PortType = PortCategory.Reset then
                        [x.DummyTag] |> List.choose id
                    else 
                        x.PLCTags |> List.ofSeq
                else 
                    x.PLCTags |> List.ofSeq
            
            match coils.isEmpty() with
            | false -> coils
            | true -> failwithlogf "%A의 태그가 존재하지않습니다." x.Parent.Name



        
        

        
        
        

        
        
        

        
        
        

        
        
        
