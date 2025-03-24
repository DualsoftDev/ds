namespace Engine.Core

open Dual.Common.Core.FS
open System
open System.Runtime.CompilerServices
open System.Reactive.Subjects
open System.Diagnostics
open System.ComponentModel

[<AutoOpen>]
module TagKindModule =
    /// Tag 에서 발생하는 event
    ///
    /// - Event{System,Flow,Vertex,ApiItem,TaskDev,HwSys,Variable} * IStorage * TagKind
    type TagEvent =
        | EventSystem   of Tag: IStorage * Target: DsSystem     * TagKind: SystemTag
        | EventFlow     of Tag: IStorage * Target: Flow         * TagKind: FlowTag
        | EventVertex   of Tag: IStorage * Target: Vertex       * TagKind: VertexTag
        | EventApiItem  of Tag: IStorage * Target: ApiItem      * TagKind: ApiItemTag
        | EventTaskDev  of Tag: IStorage * Target: TaskDev      * TagKind: TaskDevTag
        | EventHwSys    of Tag: IStorage * Target: HwSystemDef  * TagKind: HwSysTag
        | EventVariable of Tag: IStorage * Target: DsSystem     * TagKind: VariableTag
        | EventMonitor  of Tag: IStorage * Target: DsSystem     * TagKind: MonitorTag
        with
            member x.TagKind =
                match x with
                | EventSystem   (_, _, kind) -> int kind
                | EventFlow     (_, _, kind) -> int kind
                | EventVertex   (_, _, kind) -> int kind
                | EventApiItem  (_, _, kind) -> int kind
                | EventTaskDev  (_, _, kind) -> int kind
                | EventHwSys    (_, _, kind) -> int kind
                | EventVariable (_, _, kind) -> int kind
                | EventMonitor  (_, _, kind) -> int kind
                
    let TagEventSubject = new Subject<TagEvent>()
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
                @ EnumEx.Extract<MonitorTag>(ft)

    let allTagKindWithTypes = getTagKindFullSet true
    let allTagKinds = getTagKindFullSet false |> dict

    let getStorageName (fqdn:FqdnObject) (tagKind:TagKind) =
        $"{fqdn.QualifiedName}_{allTagKinds[tagKind]}" |> validStorageName
    let getTagKindName (tagKind:TagKind) = allTagKinds[tagKind]


[<AutoOpen>]
[<Extension>]
type TagKindExt =

    [<Extension>] static member OnChanged (tagDS:TagEvent) = TagEventSubject.OnNext(tagDS)
    [<Extension>] static member GetSystemTagKind    (x:IStorage) = DU.getEnumValue<SystemTag>    (x.TagKind)
    [<Extension>] static member GetFlowTagKind      (x:IStorage) = DU.getEnumValue<FlowTag>      (x.TagKind)
    [<Extension>] static member GetVertexTagKind    (x:IStorage) = DU.getEnumValue<VertexTag>    (x.TagKind)
    [<Extension>] static member GetApiTagKind       (x:IStorage) = DU.getEnumValue<ApiItemTag>   (x.TagKind)
    [<Extension>] static member GetTaskDevTagKind   (x:IStorage) = DU.getEnumValue<TaskDevTag>   (x.TagKind)
    [<Extension>] static member GetHwSysTagKind     (x:IStorage) = DU.getEnumValue<HwSysTag>     (x.TagKind)
    [<Extension>] static member GetMonitorTagKind     (x:IStorage) = DU.getEnumValue<MonitorTag>     (x.TagKind)

    
    [<Extension>] static member GetVariableTagKind  (x:IStorage) = DU.getEnumValue<VariableTag>  (x.TagKind)
    [<Extension>] static member GetAllTagKinds () : TagKindTuple array = allTagKindWithTypes
    [<Extension>] static member TryGetTaskDevTagKind  (x:IStorage) = DU.tryGetEnumValue<TaskDevTag>   (x.TagKind)
    [<Extension>] static member TryGetSystemTagKind   (x:IStorage) = DU.tryGetEnumValue<SystemTag>    (x.TagKind)
    [<Extension>] static member TryGetVariableTagKind (x:IStorage) = DU.tryGetEnumValue<VariableTag>  (x.TagKind)

    [<Extension>]
    static member GetTagInfo (x:IStorage) =
        match x.Target with
        | Some obj ->
            match obj with
            | :? DsSystem as s ->
                match x.TryGetVariableTagKind() with
                | Some v -> EventVariable(x, s, v)
                | None ->   EventSystem  (x, s, x.GetSystemTagKind())
                |> Some

            | :? Flow        as f -> Some( EventFlow    (x, f, x.GetFlowTagKind()))
            | :? Vertex      as v -> Some( EventVertex  (x, v, x.GetVertexTagKind()))
            | :? ApiItem     as a -> Some( EventApiItem (x, a, x.GetApiTagKind()))
            | :? TaskDev     as d -> Some( EventTaskDev (x, d, x.GetTaskDevTagKind()))
            | :? HwSystemDef as h -> Some( EventHwSys   (x, h, x.GetHwSysTagKind()))
            | _ -> None
        | None -> None

    [<Extension>]
    static member GetStorage(x:TagEvent) =
        match x with
        | EventSystem    (i, _, _) -> i
        | EventFlow      (i, _, _) -> i
        | EventVertex    (i, _, _) -> i
        | EventApiItem   (i, _, _) -> i
        | EventTaskDev   (i, _, _) -> i
        | EventHwSys     (i, _, _) -> i
        | EventVariable  (i, _, _) -> i
        | EventMonitor   (i, _, _) -> i
        
    [<Extension>]
    static member GetTarget(x:TagEvent) =
        match x with
        | EventSystem    ( _, target, _) -> box target
        | EventFlow      ( _, target, _) -> box target
        | EventVertex    ( _, target, _) -> box target
        | EventApiItem   ( _, target, _) -> box target
        | EventTaskDev   ( _, target, _) -> box target
        | EventHwSys     ( _, target, _) -> box target
        | EventVariable  ( _, target, _) -> box target
        | EventMonitor   ( _, target, _) -> box target

    [<Extension>]
    static member GetTagContents(x:TagEvent) =
        match x with
        | EventSystem    (tag, target, kind) -> tag.Name, tag.BoxedValue, target.Name, int kind
        | EventFlow      (tag, target, kind) -> tag.Name, tag.BoxedValue, target.Name, int kind
        | EventVertex    (tag, target, kind) -> tag.Name, tag.BoxedValue, target.Name, int kind
        | EventApiItem   (tag, target, kind) -> tag.Name, tag.BoxedValue, target.Name, int kind
        | EventTaskDev   (tag, target, kind) -> tag.Name, tag.BoxedValue, target.Name, int kind
        | EventHwSys     (tag, target, kind) -> tag.Name, tag.BoxedValue, target.Name, int kind
        | EventVariable  (tag, target, kind) -> tag.Name, tag.BoxedValue, target.Name, int kind
        | EventMonitor   (tag, target, kind) -> tag.Name, tag.BoxedValue, target.Name, int kind


    [<Extension>]
    static member GetTagToText(x:TagEvent) =
        let tagName, value, _objName, _kind = x.GetTagContents()
        $"{tagName}({value})"

    [<Extension>]
    static member GetTagToHMIText(x:TagEvent) =
        match x with
        | EventVertex (_, _, kind) ->
            match kind with
            | VertexTag.errorAction -> "디바이스이상발생"
            | VertexTag.errorWork   -> "공정이상발생"
            | VertexTag.workErrOriginGoing -> "작업원위치필요"
            | _ -> x.GetTagToText();

        | EventSystem (_, _, kind) ->
            match kind with
            | SystemTag.emergencyMonitor -> "비상버튼눌림"
            | _ -> x.GetTagToText();
        | _->
            x.GetTagToText();

    [<Extension>]
    static member GetSystem(x:TagEvent) =
        match x with
        | EventSystem    (_, target, _) -> target
        | EventFlow      (_, target, _) -> target.System
        | EventVertex    (_, target, _) -> target.Parent.GetSystem()
        | EventApiItem   (_, target, _) -> target.ApiSystem           //active system이 아니고 loaded 시스템
        | EventTaskDev   (_, target, _) -> target.ParentSystem
        | EventHwSys     (_, target, _) -> target.System
        | EventVariable  (_, target, _) -> target
        | EventMonitor   (_, target, _) -> target

    [<Extension>]
    static member IsStatusTag(x:TagEvent) =
        match x with
        | EventVertex (_, _, kind) ->
            kind.IsOneOf( VertexTag.ready
                        , VertexTag.going
                        , VertexTag.finish
                        , VertexTag.homing)
        | _ -> false

    [<Extension>]
    static member IsTagForRedisActionOutput(x:TagEvent) =
        match x with
        | EventTaskDev (_, _, kind) ->
            kind.IsOneOf(
                TaskDevTag.actionOut
            )
        | _ -> false

    [<Extension>]
    static member IsVertexTokenTag(x:TagEvent) =
        match x with
        | EventVertex (_, _, kind) ->
            kind.IsOneOf(
                VertexTag.realToken
                , VertexTag.sourceToken
                , VertexTag.mergeToken
            )
        | _ -> false

    [<Extension>]
    static member IsVertexErrTag(x:TagEvent) =
        match x with
        | EventVertex (_, _, kind) ->
            kind.IsOneOf(
                  VertexTag.errorAction
                , VertexTag.errorWork
                , VertexTag.rxErrInterlock
                , VertexTag.rxErrOpen
                , VertexTag.rxErrShort
                , VertexTag.workErrOriginGoing

                , VertexTag.txErrOnTimeOver
                , VertexTag.txErrOnTimeUnder
                , VertexTag.txErrOffTimeOver
                , VertexTag.txErrOffTimeUnder
                )
        | _ -> false

    

    [<Extension>]
    static member IsVertexOpcDataTag(x:IStorage) =
        x.TagKind.IsOneOf(
              int FlowTag.drive_state,
              int FlowTag.error_state,
              int FlowTag.pause_state,
              int FlowTag.emergency_state,
              int FlowTag.going_state,
              int FlowTag.origin_state,
              int FlowTag.ready_state,
              int FlowTag.test_state,
              int FlowTag.idle_mode,
              
              int VertexTag.calcStatActionFinish,
              int VertexTag.planStart,
              int VertexTag.startTag, 
              int VertexTag.finish, 
              int VertexTag.endTag)


    [<Extension>]
    static member IsSystemErrTag(x:TagEvent) =
        match x with
        | EventSystem (_, _, kind) ->
            kind.IsOneOf(
                 SystemTag.emergencyMonitor
                )
        | _ -> false


    [<Extension>]
    static member IsSystemConditionErrTag(x:TagEvent) =
        match x with
        | EventHwSys (_, _, kind) ->
            kind.IsOneOf(
                   HwSysTag.HwDriveConditionErr
                 , HwSysTag.HwReadyConditionErr
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
    static member IsActionOutStg(x:IStorage) =
        x.TagKind.IsOneOf(
               int TaskDevTag.actionOut
              )

    [<Extension>]
    static member IsMonitorStg(x:IStorage) =
        x.TagKind.IsOneOf(
               int MonitorTag.UserTagType
              )

    [<Extension>]
    static member IsMotionEndStg(x:IStorage) =
        x.TagKind.IsOneOf(
               int VertexTag.motionEnd
              )
    [<Extension>]
    static member IsScriptEndStg(x:IStorage) =
        x.TagKind.IsOneOf(
               int VertexTag.scriptEnd
              )
              
    [<Extension>]
    static member IsMotionEnd(x:TagEvent) =
        match x with
        | EventVertex (s, _, _) -> s.IsMotionEndStg()
        | _ -> false

    [<Extension>]
    static member IsScriptEnd(x:TagEvent) =
        match x with
        | EventVertex (s, _, _) -> s.IsScriptEndStg()
        | _ -> false


    [<Extension>]
    static member IsPlanEndTag(x:IStorage) =
        x.TagKind.IsOneOf(
                int VertexTag.planEnd
              )

    [<Extension>]
    static member IsNeedGraphUI(x:TagEvent) =
        if x.IsVertexErrTag() then
            true
        else
            match x with
            | EventVertex (_, _, kind) ->
                kind.IsOneOf(
                      VertexTag.ready
                    , VertexTag.going
                    , VertexTag.finish
                    , VertexTag.homing
                    , VertexTag.pause
                    , VertexTag.origin
                    , VertexTag.callIn
                    , VertexTag.callOut
                    , VertexTag.planEnd
                    )

            | EventApiItem (_, _, _) -> true
            | EventTaskDev (_, _, _) -> true
            | _ -> false



    [<Extension>]
    static member IsNeedSaveDBLog(x:TagEvent) =
        if x.IsVertexErrTag() then
            true
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
                      TaskDevTag.actionIn
                    , TaskDevTag.actionOut
                    )
            | EventHwSys (_, _, _) -> false
            | EventVariable (_, _, _) -> true
            | EventMonitor (_, _, _) -> true
