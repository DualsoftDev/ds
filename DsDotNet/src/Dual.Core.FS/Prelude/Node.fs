namespace Dual.Core.Prelude

open Dual.Common
open Dual.Core.Prelude
open System
open Newtonsoft.Json
open System.Collections.Generic
open System.Runtime.CompilerServices
open System.Linq
open System.Diagnostics

[<AutoOpen>]
module internal Prelude =
    let toNodeSeq (lst: 'a seq) =
        lst |> Seq.cast<INode>

    /// children 의 parent 를 설정
    let setParent (parent:#INodeStem) (children: #INode seq) =
        children
        |> Seq.iter (fun n -> n.Parent <- Some(parent :> INodeStem))


/// 이름을 갖는 객체의 interface
type INamed =
    abstract member Name:string with get, set

/// ToText 지원 interface
type IText =
    abstract member ToText:unit -> string

type ITag =
    inherit INamed
    abstract member Address:string with get

/// Project, Area, Device 의 base interface
type IPAD =
    inherit INode
    inherit INamed

type IProperties =
    abstract member GetProperties:unit -> Dictionary<String, obj>
    abstract member SetProperties:Dictionary<String, obj> -> unit


/// 객체 (Node 등) 복사시의 child 복사 방식 설정
type ChildCopyDepth =
    /// do not copy children
    | DoNotCopy
    /// share reference to children
    | Shallow
    /// deep copy children : with shift parameter : address shift 에 사용됨
    /// * None 인 경우, 복사된 node 의 address 영역을 None/Null 으로 설정
    /// * 0 인 경우, address 를 동일한 값으로 복제
    /// * n 인 경우, address 를 n 만큼 shift 시킨 값으로 복제
    | Deep of int option

/// 객체 (Node 등) 복사시의 option
type CopyPreference(childCopyOption) =
    new() = CopyPreference(Shallow)
    member val ChildCopyOption:ChildCopyDepth = childCopyOption with get, set
    static member CreateShallowCopy() = CopyPreference(Shallow)
    static member CreateDeepCopy(depth) = CopyPreference(depth)
    static member CreateDoNotCopy() = CopyPreference(DoNotCopy)

/// 복사 가능한 객체의 interface
type IClonable =
    abstract member Clone : opt:CopyPreference -> IClonable

/// 이름을 갖는 객체의 base class
/// 속성 멤버 이름 변경시 JSON serialize 에 영향을 미침
type NamedObject(name)=
    member val Name:string = name with get, set

/// NamedObject + INamed implementation
type NamedInterfaceObject(name) =
    inherit NamedObject(name)
    interface INamed with
        member x.Name with get() = x.Name and set(v) = x.Name <- v


/// DS domain model node
[<DebuggerDisplay("{ToText()}")>]
type Node(parent:INodeStem option, properties:array<string*obj>, id:string) =

    let mutable parent = parent
//    let event = Event<_,_>()
    /// Properties 속성 dictionary
    let mutable properties = properties |> dict |> Dictionary

    do
        if not (properties.ContainsKey(K.InstanceId)) then
            properties.Add(K.InstanceId, id)
        properties.[K.InstanceId] <- Guid.NewGuid().ToString()

    interface INode with
        member x.Parent with get() = x.Parent and set(v) = x.Parent <- v
    [<JsonIgnore>]
    member x.Parent with get()= parent and set(v) = parent <- v

    interface IClonable with
        member x.Clone opt = x.Clone opt :?> IClonable

    abstract member Clone : opt:CopyPreference -> INode
    abstract member CloneSelf : opt:CopyPreference -> INode
    abstract member Rename : name:string -> bool
    /// 상속받은 클래스에서 구현해야 할 method.  구현하지 않으면 fail
    default x.Clone opt = failwithlogf "Clone not implemented!"
    /// 상속받은 클래스에서 구현해야 할 method.  구현하지 않으면 fail
    default x.CloneSelf opt = failwithlogf "CloneSelf not implemented!"
    default x.Rename (name:string) =
        //let parent = x.Parent |> Option.get :?> NodeStem
        //(parent.getTypedCollection x).Rename x name
        failwith "ERROR"
        false

    member x.Properties       
        with get():ResizeArray<string*_> = properties.ToList() |> Seq.map (|KeyValue|) |> ResizeArray.ofSeq 
        and set(v:ResizeArray<string*_>) = 
            let v = v |> dict |> Dictionary
            x.SetProperties(v)

    member internal x.PropertiesToDict with get() = properties and set(v) = properties <- v

    /// Set 할때 조작
    abstract member SetProperties:Dictionary<string,obj> -> unit 
    default x.SetProperties(v:Dictionary<string,obj>) = x.PropertiesToDict <- v 

    /// Get 할때 조작
    abstract member GetProperties: unit -> Dictionary<string,obj>
    default x.GetProperties() = properties

    interface IProperties with
        member x.GetProperties() = x.GetProperties()
        member x.SetProperties(v) = x.SetProperties(v)

    member x.Id         
        with get() = 
            if x.PropertiesToDict.[K.InstanceId] :?> string  = String.Empty || x.PropertiesToDict.[K.InstanceId] = null 
            then x.PropertiesToDict.[K.InstanceId] <- Guid.NewGuid().ToString()
            x.PropertiesToDict.[K.InstanceId] :?> string
        and set(v:string) = x.PropertiesToDict.[K.InstanceId] <- v

    abstract ToText : unit -> string
    override x.ToText() = ""
    override x.ToString() = x.ToText()



and NodeLeaf(parent:INodeStem option, properties:array<string*obj>, id) =
    inherit Node(parent, properties, id)
    interface INodeLeaf with
        member x.Parent with get() = x.Parent and set(v) = x.Parent <- v

    /// Leaf 에서 상속받은 PLCTag, Dummy 는 CloneSelf 만 구현하면 됨
    override x.Clone opt = x.CloneSelf opt
#if false
and NodeStem(sections:ITypedCollection seq, parent:INodeStem option, properties:array<string*obj>, id) as this =
    inherit Node(parent, properties, id)
    /// Section(Type 별 child 콜렉션) 관리를 위한 map
    /// Type * Section 
    let sectionMap = sections |> Seq.map (fun sec -> sec.Type, sec) |> dict
    /// Section map 에 주어진 obj type 이 존재하는지 검사
    let checkType obj = sectionMap.ContainsKey(obj.GetType())
    let checkTypes objs = objs |> Seq.forall checkType
    /// child 를 자식으로 받아 들임
    let adopt (child:INode) = child.Parent <- Some (this :> INodeStem)
    /// child 를 자식에서 제외
    let abandon (child:INode) = child.Parent <- None
        
    new(sections) = NodeStem(sections, None, [||], null)

    member internal x.Adopt (child:INode) = adopt child

    abstract member Children : INode seq with get, set
    /// 직접 child get, set
    default x.Children
        with get() =
            sectionMap.Values
            |> Seq.collect(fun sec -> sec.AllObjects)
            |> Seq.cast<INode>
        and set(vv) =
            assert(checkTypes vv)
            vv |> Seq.iter adopt
            for (k, vs) in vv |> Seq.groupBy(fun v -> v.GetType()) do
                sectionMap.[k].Initialize(vs)

    /// Children 중에서 주어진 type 만 골라 냄
    member x.GetChildrenOfType<'t>() =
        let typ = typedefof<'t>
        if sectionMap.ContainsKey typ then
            sectionMap.[typ].AllObjects |> Seq.cast<'t>
        else
            failwithlogf "Failed to get children.  Type %A not found." typ
            Seq.empty
    member x.SetChildrenOfType<'t(*when 't :> INode*)> (children:'t seq) =
        let typ = typedefof<'t>
        if sectionMap.ContainsKey typ then
            sectionMap.[typ].Initialize (toNodeSeq(children))
        else
            failwithlogf "Failed to set children.  Type %A not found." typ

    member x.ContainType(child:INode) =
        sectionMap.ContainsKey(child.GetType())

    member internal x.SectionMap:Collections.Generic.IDictionary<Type,ITypedCollection> = sectionMap
    interface INodeStem with
        member x.Parent with get() = x.Parent and set(v) = x.Parent <- v
        member x.Children with get() = x.Children and set(v) = x.Children <- v

    abstract member Add : child:INode -> bool
    abstract member AddRange : child:INode seq -> bool
    abstract member Remove : child:INode -> bool
    abstract member Replace : oldChild:INode -> newChild:INode -> bool
    abstract member getTypedCollection : INode -> ITypedCollection
    abstract member Exists: child:INode -> bool
    
    default x.getTypedCollection child = 
        sectionMap.[child.GetType()]
    /// Child 추가.  child 의 parent 설정 포함.
    default x.Add (child:INode) =
        if (x.getTypedCollection child).Add(child)
        then
            adopt child
            true
        else false

    default x.AddRange(children:INode seq) =
        //let checkChildrenTypes = checkTypes children
        let checkDistinctName = children |> Seq.exists(fun c -> (x.getTypedCollection c).Exists(c)) |> not

        if((*checkChildrenTypes && *)checkDistinctName)
        then 
            children |> Seq.iter(fun c -> x.Add(c) |> ignore) 
            true
        else
            false
            

    /// Child 제거.  child 의 parent null 설정 포함.
    default x.Remove(child:INode) =
        if (x.getTypedCollection child).Remove(child)
        then
            abandon child
            true
        else false

    default x.Replace(oldChild:INode) (newChild:INode) =
        if oldChild.GetType().Equals(newChild.GetType()) && (x.getTypedCollection oldChild).Replace oldChild newChild
        then
            abandon oldChild
            adopt newChild
            true
        else false

    default x.Exists(child:INode) =
        (x.getTypedCollection child).Exists child

    /// Stem 의 (범용) 복사
    /// Stem 에서 상속받은 DsFile, DsProject, ..., 는 CloneSelf 만 구현하면 됨
    override x.Clone opt =
        let self = x.CloneSelf opt :?> NodeStem
        match opt.ChildCopyOption with
        | DoNotCopy ->
            ()
        | Shallow | Deep(_) ->
            self.Children <-
                x.Children
                |> Seq.cast<Node>
                |> Seq.map (fun ch -> ch.Clone opt)
                |> Array.ofSeq
        self :> INode


/// NodeStem + INamed 
/// Work, File, Project, Area, Device, Library, Book 등의 base class
/// Child 의 type 별로 collection 을 관리
type NamedNodeStem(name:string, sections:ITypedCollection seq, parent:INodeStem option, properties:array<string*obj>, id) as this =
    inherit NodeStem(sections, parent, properties, id)
    //let named = NamedInterfaceObject(name, origname)

    do
        if not (this.PropertiesToDict.ContainsKey(K.Name)) then
            this.PropertiesToDict.Add(K.Name, box(name))

    new(name, sections, id) = NamedNodeStem(name, sections, None, [||], id)
    new(name, sections, properties, id) = NamedNodeStem(name, sections, None, properties, id)

    interface INamed with
        member x.Name with get() = x.Name and set(v) = x.Name <- v
    member x.Name 
        with get() = x.PropertiesToDict.[K.Name] :?> string 
        and set(v:string) = x.PropertiesToDict.[K.Name] <- v
#endif

/// NodeLeaf + INamed implementation
/// PLCTag, Dummy 의 base class
type NamedNodeLeaf(name:string, parent:INodeStem option, properties:array<string*obj>, id) as this =
    inherit NodeLeaf(parent, properties, id)

    do
        if not (this.PropertiesToDict.ContainsKey(K.Name)) then
            this.PropertiesToDict.Add(K.Name, box(name))

    new(name) = NamedNodeLeaf(name, None, [||], null)

    interface INamed with
        member x.Name with get() = x.Name and set(v) = x.Name <- v
    member x.Name 
        with get() = x.PropertiesToDict.[K.Name] :?> string 
        and set(v:string) = x.PropertiesToDict.[K.Name] <- v
    override x.ToText() = x.Name


module internal NamedNodeForwardM =
    let getFullName = ForwardDecl.declare<NamedNodeLeaf -> string -> string>
    //let getStemFullName = ForwardDecl.declare<NamedNodeStem -> string -> string>
