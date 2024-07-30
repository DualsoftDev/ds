namespace Engine.Core

open Dual.Common.Core.FS
open System
open System.Runtime.CompilerServices
open System.Reactive.Subjects

[<AutoOpen>]
module TagKindModule =

    

    type TagDS =
        | EventSystem   of Tag: IStorage * Target: DsSystem     * TagKind: SystemTag
        | EventFlow     of Tag: IStorage * Target: Flow         * TagKind: FlowTag
        | EventVertex   of Tag: IStorage * Target: Vertex       * TagKind: VertexTag
        | EventApiItem  of Tag: IStorage * Target: ApiItem      * TagKind: ApiItemTag
        | EventTaskDev  of Tag: IStorage * Target: TaskDev      * TagKind: TaskDevTag
        | EventHwSys    of Tag: IStorage * Target: HwSystemDef  * TagKind: HwSysTag
        | EventVariable of Tag: IStorage * Target: DsSystem     * TagKind: VariableTag
        

    let TagDSSubject = new Subject<TagDS>()
    type TagKind = int
    type TagKindTuple = TagKind * string        //TagKind, TagKindName
    
    type EnumEx() =
        static member Extract<'T when 'T: struct>(formatWithType: bool) : TagKindTuple array =
            let typ = typeof<'T>
            let values = Enum.GetValues(typ) :?> 'T[] |> Seq.cast<int> |> Array.ofSeq
            let names = 
                Enum.GetNames(typ) 
                |> Array.map (fun n -> if formatWithType then $"{typ.Name}.{n}" else n)
            Array.zip values names

    let getTagKindFullSet(ft:bool) = //formatWithType
            EnumEx.Extract<SystemTag>(ft)
                @ EnumEx.Extract<FlowTag>(ft)
                @ EnumEx.Extract<VertexTag>(ft)
                @ EnumEx.Extract<ApiItemTag>(ft)
                @ EnumEx.Extract<TaskDevTag>(ft)
                @ EnumEx.Extract<HwSysTag>(ft)
                @ EnumEx.Extract<VariableTag>(ft)

    let allTagKindWithTypes = getTagKindFullSet true
    let allTagKinds = getTagKindFullSet false |> dict
                
    let getStorageName (fqdn:FqdnObject) (tagKind:TagKind) =
        $"{fqdn.QualifiedName}_{allTagKinds[tagKind]}" |> validStorageName
    

[<AutoOpen>]
[<Extension>]
type TagKindExt =
    [<Extension>] static member OnChanged (tagDS:TagDS) = TagDSSubject.OnNext(tagDS)
    [<Extension>] static member GetSystemTagKind    (x:IStorage) = DU.tryGetEnumValue<SystemTag>(x.TagKind)
    [<Extension>] static member GetFlowTagKind      (x:IStorage) = DU.tryGetEnumValue<FlowTag>(x.TagKind)
    [<Extension>] static member GetVertexTagKind    (x:IStorage) = DU.tryGetEnumValue<VertexTag>(x.TagKind)
    [<Extension>] static member GetApiTagKind       (x:IStorage) = DU.tryGetEnumValue<ApiItemTag>(x.TagKind)
    [<Extension>] static member GetTaskDevTagKind   (x:IStorage) = DU.tryGetEnumValue<TaskDevTag>(x.TagKind)
    [<Extension>] static member GetHwSysTagKind     (x:IStorage) = DU.tryGetEnumValue<HwSysTag>(x.TagKind)
    [<Extension>] static member GetVariableTagKind  (x:IStorage) = DU.tryGetEnumValue<VariableTag>(x.TagKind)
    [<Extension>] static member GetAllTagKinds () : TagKindTuple array = allTagKindWithTypes

    [<Extension>]
    static member GetTagInfo (x:IStorage) =
        match x.Target with
        | Some obj ->
            match obj with
            | :? DsSystem as s ->
                if DU.tryGetEnumValue<VariableTag>(x.TagKind).IsSome then
                    Some( EventVariable(x, s, x.GetVariableTagKind().Value))
                else
                    Some( EventSystem(x, s, x.GetSystemTagKind().Value))

            | :? Flow as f         -> Some( EventFlow    (x, f, x.GetFlowTagKind().Value))
            | :? Vertex as v       -> Some( EventVertex  (x, v, x.GetVertexTagKind().Value))
            | :? ApiItem as a      -> Some( EventApiItem (x, a, x.GetApiTagKind().Value))
            | :? TaskDev  as d     -> Some( EventTaskDev (x, d, x.GetTaskDevTagKind().Value))
            | :? HwSystemDef as h  -> Some( EventHwSys   (x, h, x.GetHwSysTagKind().Value))
            | _ -> None
        | None -> None
   
    [<Extension>]
    static member GetStorage(x:TagDS) =
        match x with
        | EventSystem    (i, _, _) -> i
        | EventFlow      (i, _, _) -> i
        | EventVertex    (i, _, _) -> i       
        | EventApiItem   (i, _, _) -> i
        | EventTaskDev   (i, _, _) -> i
        | EventHwSys     (i, _, _) -> i
        | EventVariable  (i, _, _) -> i




    [<Extension>]
    static member GetTarget(x:TagDS) =
        match x with
        | EventSystem    ( _, target, _) -> target |> box
        | EventFlow      ( _, target, _) -> target |> box
        | EventVertex    ( _, target, _) -> target |> box
        | EventApiItem   ( _, target, _) -> target |> box
        | EventTaskDev   ( _, target, _) -> target |> box
        | EventHwSys     ( _, target, _) -> target |> box
        | EventVariable  ( _, target, _) -> target |> box

    [<Extension>]
    static member GetTagContents(x:TagDS) =
        match x with
        | EventSystem    (tag, obj, kind) -> tag.Name,tag.BoxedValue, obj.Name, kind|>int
        | EventFlow      (tag, obj, kind) -> tag.Name,tag.BoxedValue, obj.Name, kind|>int
        | EventVertex    (tag, obj, kind) -> tag.Name,tag.BoxedValue, obj.Name, kind|>int
        | EventApiItem   (tag, obj, kind) -> tag.Name,tag.BoxedValue, obj.Name, kind|>int
        | EventTaskDev   (tag, obj, kind) -> tag.Name,tag.BoxedValue, obj.Name, kind|>int
        | EventHwSys     (tag, obj, kind) -> tag.Name,tag.BoxedValue, obj.Name, kind|>int
        | EventVariable  (tag, obj, kind) -> tag.Name,tag.BoxedValue, obj.Name, kind|>int
        
     
    [<Extension>]
    static member GetTagToText(x:TagDS) = 
        let tagName, value, _objName, _kind = x.GetTagContents()     
        $"{tagName}({value})"

    [<Extension>]
    static member GetSystem(x:TagDS) =
        match x with
        | EventSystem    (_, obj, _) -> obj
        | EventFlow      (_, obj, _) -> obj.System
        | EventVertex    (_, obj, _) -> obj.Parent.GetSystem()       
        | EventApiItem   (_, obj, _) -> obj.ApiSystem           //active system이 아니고 loaded 시스템
        | EventTaskDev   (_, obj, _) -> obj.ParnetSystem
        | EventHwSys     (_, obj, _) -> obj.System
        | EventVariable  (_, obj, _) -> obj
        
    [<Extension>]
    static member IsStatusTag(x:TagDS) =
        match x with
        | EventVertex (_, _, kind) ->
            kind.IsOneOf( VertexTag.ready
                        , VertexTag.going
                        , VertexTag.finish
                        , VertexTag.homing)
        | _ -> false

    [<Extension>]
    static member IsVertexOriginTag(x:TagDS) =
        match x with
        | EventVertex (_, _, kind) ->  kind.IsOneOf(   VertexTag.origin
                                                    )
        | _ -> false


    [<Extension>]
    static member IsVertexErrTag(x:TagDS) =
        match x with
        | EventVertex (_, _, kind) ->
            kind.IsOneOf(  
                  VertexTag.errorTRx
                , VertexTag.rxErrOpen
                , VertexTag.rxErrShort
                , VertexTag.workErrOriginGoing
                                                    
                , VertexTag.txErrOnTimeOver
                , VertexTag.txErrOnTimeShortage  
                , VertexTag.txErrOffTimeOver
                , VertexTag.txErrOffTimeShortage
                )
        | _ -> false

    [<Extension>]
    static member IsStatusTag(x:IStorage) =
        x.TagKind.IsOneOf(
              int VertexTag.ready
            , int VertexTag.going
            , int VertexTag.finish
            , int VertexTag.homing)




    [<Extension>]
    static member IsNeedSaveDBLog(x:TagDS) =
        if x.IsVertexErrTag()
        then true
        else 
            match x with
            | EventSystem (_, _, kind) ->
                kind.IsOneOf(  
                      SystemTag.autoMonitor     
                    , SystemTag.manualMonitor   
                    , SystemTag.driveMonitor    
                    , SystemTag.errorMonitor     
                    , SystemTag.emergencyMonitor      
                    , SystemTag.testMonitor     
                    , SystemTag.readyMonitor    
                    , SystemTag.pauseMonitor
                    , SystemTag.clear_btn)

            | EventFlow (_, _, kind) ->
                kind.IsOneOf(
                      FlowTag.drive_state
                    , FlowTag.pause_state
                    , FlowTag.flowStopError
                    )

            | EventVertex (_, _, kind) ->
                kind.IsOneOf(
                      VertexTag.ready
                    , VertexTag.going
                    , VertexTag.finish
                    , VertexTag.homing
                    , VertexTag.pause       
                    )
                                          
            | EventApiItem (_, _, _) -> false

            | EventTaskDev (_, _, kind) ->
                kind.IsOneOf(
                      TaskDevTag.planStart
                    , TaskDevTag.planEnd
                    , TaskDevTag.actionIn
                    , TaskDevTag.actionOut
                    , TaskDevTag.actionMemory
                    )
            | EventHwSys (_, _, _) -> false
            | EventVariable (_, _, _) -> true
