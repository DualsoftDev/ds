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
        | EventAction   of Tag: IStorage * Target: TaskDev      * TagKind: ActionTag
        | EventHwSys    of Tag: IStorage * Target: HwSystemItem * TagKind: HwSysTag
        | EventVariable of Tag: IStorage * Target: DsSystem     * TagKind: VariableTag
        | EventJob      of Tag: IStorage * Target: Job          * TagKind: JobTag
        

    let TagDSSubject = new Subject<TagDS>()
    type TagKind = int
    type TagKindTuple = TagKind * string
    
    type EnumEx() =
        static member Extract<'T when 'T: struct>() : TagKindTuple array =
            let typ = typeof<'T>
            let values = Enum.GetValues(typ) :?> 'T[] |> Seq.cast<int> |> toArray

            let names = Enum.GetNames(typ) |> map (fun n -> $"{typ.Name}.{n}")
            Array.zip values names


[<AutoOpen>]
[<Extension>]
type TagKindExt =
    [<Extension>] static member OnChanged (tagDS:TagDS) = TagDSSubject.OnNext(tagDS)
    [<Extension>] static member GetSystemTagKind    (x:IStorage) = DU.tryGetEnumValue<SystemTag>(x.TagKind)
    [<Extension>] static member GetFlowTagKind      (x:IStorage) = DU.tryGetEnumValue<FlowTag>(x.TagKind)
    [<Extension>] static member GetVertexTagKind    (x:IStorage) = DU.tryGetEnumValue<VertexTag>(x.TagKind)
    [<Extension>] static member GetApiTagKind       (x:IStorage) = DU.tryGetEnumValue<ApiItemTag>(x.TagKind)
    [<Extension>] static member GetActionTagKind    (x:IStorage) = DU.tryGetEnumValue<ActionTag>(x.TagKind)
    [<Extension>] static member GetHwSysTagKind     (x:IStorage) = DU.tryGetEnumValue<HwSysTag>(x.TagKind)
    [<Extension>] static member GetVariableTagKind  (x:IStorage) = DU.tryGetEnumValue<VariableTag>(x.TagKind)
    [<Extension>] static member GetJobTagKind       (x:IStorage) = DU.tryGetEnumValue<JobTag>(x.TagKind)
    [<Extension>] static member GetAllTagKinds () : TagKindTuple array =
                    EnumEx.Extract<SystemTag>()
                    @ EnumEx.Extract<FlowTag>()
                    @ EnumEx.Extract<VertexTag>()
                    @ EnumEx.Extract<ApiItemTag>()
                    @ EnumEx.Extract<ActionTag>()
                    @ EnumEx.Extract<HwSysTag>()
                    @ EnumEx.Extract<VariableTag>()
                    @ EnumEx.Extract<JobTag>()

    [<Extension>]
    static member GetTagInfo (x:IStorage) =
        match x.Target with
        |Some obj ->
            match obj with
            | :? DsSystem as s     ->  if DU.tryGetEnumValue<VariableTag>(x.TagKind).IsSome
                                       then Some( EventVariable  (x, s, x.GetVariableTagKind().Value))
                                       else Some( EventSystem  (x, s, x.GetSystemTagKind().Value))

            | :? Flow as f         ->Some( EventFlow    (x, f, x.GetFlowTagKind().Value))
            | :? Vertex as v       ->Some( EventVertex  (x, v, x.GetVertexTagKind().Value))
            | :? ApiItem as a      ->Some( EventApiItem (x, a, x.GetApiTagKind().Value))
            | :? TaskDev  as d      ->Some( EventAction  (x, d, x.GetActionTagKind().Value))
            | :? HwSystemItem as h ->Some( EventHwSys   (x, h, x.GetHwSysTagKind().Value))
            | :? Job as j          ->Some( EventJob     (x, j, x.GetJobTagKind().Value))
            |_ -> None
        |None -> None
   
    [<Extension>]
    static member GetStorage(x:TagDS) =
        match x with
        |EventSystem    (i, _, _) -> i
        |EventFlow      (i, _, _) -> i
        |EventVertex    (i, _, _) -> i       
        |EventApiItem   (i, _, _) -> i
        |EventAction    (i, _, _) -> i
        |EventHwSys     (i, _, _) -> i
        |EventVariable  (i, _, _) -> i
        |EventJob       (i, _, _) -> i




    [<Extension>]
    static member GetTarget(x:TagDS) =
        match x with
        |EventSystem    ( _, target, _) -> target |> box
        |EventFlow      ( _, target, _) -> target |> box
        |EventVertex    ( _, target, _) -> target |> box
        |EventApiItem   ( _, target, _) -> target |> box
        |EventAction    ( _, target, _) -> target |> box
        |EventHwSys     ( _, target, _) -> target |> box
        |EventVariable  ( _, target, _) -> target |> box
        |EventJob       ( _, target, _) -> target |> box

      
    [<Extension>]
    static member GetTagToText(x:TagDS) =
        let getText(tag:IStorage) (obj:INamed) kind = $"{tag.Name};{tag.BoxedValue};{obj.Name};{kind}"
        match x with
        |EventSystem    (tag, obj, kind) -> getText tag obj kind
        |EventFlow      (tag, obj, kind) -> getText tag obj kind
        |EventVertex    (tag, obj, kind) -> getText tag obj kind
        |EventApiItem   (tag, obj, kind) -> getText tag obj kind
        |EventAction    (tag, obj, kind) -> getText tag obj kind
        |EventHwSys     (tag, obj, kind) -> getText tag obj kind
        |EventVariable  (tag, obj, kind) -> getText tag obj kind
        |EventJob       (tag, obj, kind) -> getText tag obj kind
        
    

    [<Extension>]
    static member GetSystem(x:TagDS) =
        match x with
        |EventSystem    (_, obj, _) -> obj
        |EventFlow      (_, obj, _) -> obj.System
        |EventVertex    (_, obj, _) -> obj.Parent.GetSystem()       
        |EventApiItem   (_, obj, _) -> obj.ApiSystem           //active system이 아니고 loaded 시스템
        |EventAction    (_, obj, _) -> obj.ApiItem.ApiSystem   //active system이 아니고 loaded 시스템
        |EventHwSys     (_, obj, _) -> obj.System
        |EventVariable  (_, obj, _) -> obj
        |EventJob       (_, obj, _) -> obj.System
        
    [<Extension>]
    static member IsStatusTag(x:TagDS) =
        match x with
        |EventVertex (_, _, kind) ->
            kind.IsOneOf( VertexTag.ready
                        , VertexTag.going
                        , VertexTag.finish
                        , VertexTag.homing)
        |_ -> false

    [<Extension>]
    static member IsVertexOriginTag(x:TagDS) =
        match x with
        |EventVertex (_, _, kind) ->  kind.IsOneOf(   VertexTag.origin
                                                    )
        |_->false


    [<Extension>]
    static member IsVertexErrTag(x:TagDS) =
        match x with
        |EventVertex (_, _, kind) ->  kind.IsOneOf(   VertexTag.errorTRx
                                                    , VertexTag.rxErrOpen
                                                    , VertexTag.rxErrShort
                                                    , VertexTag.txErrTimeOver
                                                    , VertexTag.txErrTimeShortage
                                                    )
        |_->false

    [<Extension>]
    static member IsStatusTag(x:IStorage) =
        x.TagKind.IsOneOf(
              int VertexTag.ready
            , int VertexTag.going
            , int VertexTag.finish
            , int VertexTag.homing)




    [<Extension>]
    static member IsNeedSaveDBLog(x:TagDS) =
        match x with
        |EventSystem (_, _, kind) ->  kind.IsOneOf(  
                                                      SystemTag.autoMonitor     
                                                    , SystemTag.manualMonitor   
                                                    , SystemTag.driveMonitor    
                                                    , SystemTag.errorMonitor     
                                                    , SystemTag.emergencyMonitor      
                                                    , SystemTag.testMonitor     
                                                    , SystemTag.readyMonitor    
                                                    , SystemTag.pauseMonitor
                                                    , SystemTag.clear_btn)

        |EventFlow   (_, _, kind) ->  kind.IsOneOf(  FlowTag.drive_state
                                                    , FlowTag.flowPause
                                                    , FlowTag.flowStopError
                                                    )

        |EventVertex (_, _, kind) ->  kind.IsOneOf(
                                        //VertexTag.ready,
                                        VertexTag.going       
                                        //, VertexTag.finish       
                                        //, VertexTag.homing       
                                        , VertexTag.pause       
                                        , VertexTag.errorTRx       
                                        , VertexTag.rxErrOpen
                                        , VertexTag.rxErrShort
                                        , VertexTag.txErrTimeOver
                                        , VertexTag.txErrTimeShortage
                                        
                                        )
                                          
        |EventApiItem (_, _, _) -> false
        |EventAction (_, _, _) -> false
        |EventHwSys  (_, _, _) -> false
        |EventVariable  (_, _, _) -> true
        |EventJob       (_, _, _) -> true
