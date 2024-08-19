namespace Engine.Core

open Dual.Common.Core.FS
open System
open System.Runtime.CompilerServices
open System.Reactive.Subjects

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
        | EventJob      of Tag: IStorage * Target: Job          * TagKind: JobTag
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
                | EventJob      (_, _, kind) -> kind |> int

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
                @ EnumEx.Extract<JobTag>(ft)

    let allTagKindWithTypes = getTagKindFullSet true
    let allTagKinds = getTagKindFullSet false |> dict

    let getStorageName (fqdn:FqdnObject) (tagKind:TagKind) =
        $"{fqdn.QualifiedName}_{allTagKinds[tagKind]}" |> validStorageName


[<AutoOpen>]
[<Extension>]
type TagKindExt =
    [<Extension>] static member OnChanged (tagDS:TagEvent) = TagEventSubject.OnNext(tagDS)
    [<Extension>] static member GetSystemTagKind    (x:IStorage) = DU.tryGetEnumValue<SystemTag>    (x.TagKind)
    [<Extension>] static member GetFlowTagKind      (x:IStorage) = DU.tryGetEnumValue<FlowTag>      (x.TagKind)
    [<Extension>] static member GetVertexTagKind    (x:IStorage) = DU.tryGetEnumValue<VertexTag>    (x.TagKind)
    [<Extension>] static member GetApiTagKind       (x:IStorage) = DU.tryGetEnumValue<ApiItemTag>   (x.TagKind)
    [<Extension>] static member GetTaskDevTagKind   (x:IStorage) = DU.tryGetEnumValue<TaskDevTag>   (x.TagKind)
    [<Extension>] static member GetHwSysTagKind     (x:IStorage) = DU.tryGetEnumValue<HwSysTag>     (x.TagKind)
    [<Extension>] static member GetVariableTagKind  (x:IStorage) = DU.tryGetEnumValue<VariableTag>  (x.TagKind)
    [<Extension>] static member GetJobTagKind       (x:IStorage) = DU.tryGetEnumValue<JobTag>       (x.TagKind)
    [<Extension>] static member GetAllTagKinds () : TagKindTuple array = allTagKindWithTypes

    [<Extension>]
    static member GetTagInfo (x:IStorage) =
        match x.Target with
        | Some obj ->
            match obj with
            | :? DsSystem as s ->
                match x.GetVariableTagKind() with
                | Some v -> EventVariable(x, s, v)
                | None ->   EventSystem  (x, s, x.GetSystemTagKind().Value)
                |> Some

            | :? Flow        as f -> Some( EventFlow    (x, f, x.GetFlowTagKind().Value))
            | :? Vertex      as v -> Some( EventVertex  (x, v, x.GetVertexTagKind().Value))
            | :? ApiItem     as a -> Some( EventApiItem (x, a, x.GetApiTagKind().Value))
            | :? TaskDev     as d -> Some( EventTaskDev (x, d, x.GetTaskDevTagKind().Value))
            | :? HwSystemDef as h -> Some( EventHwSys   (x, h, x.GetHwSysTagKind().Value))
            | :? Job         as j -> Some( EventJob     (x, j, x.GetJobTagKind().Value))
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
        | EventJob       (i, _, _) -> i

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
        | EventJob       ( _, target, _) -> target |> box

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
        | EventJob       (tag, target, kind) -> tag.Name, tag.BoxedValue, target.DequotedQualifiedName, kind|>int


    [<Extension>]
    static member GetTagToText(x:TagEvent) =
        let tagName, value, _objName, _kind = x.GetTagContents()
        $"{tagName}({value})"

    [<Extension>]
    static member GetTagToHMIText(x:TagEvent) =
        match x with
        | EventVertex (_, _, kind) ->
            match kind with
            | VertexTag.errorTRx -> "작업이상발생"
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
        | EventJob       (_, target, _) -> target.System

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
    static member IsTagForRedisMotion(x:TagEvent) =
        match x with
        | EventVertex (_, _, kind) ->
            kind.IsOneOf( VertexTag.scriptStart
                        , VertexTag.motionStart
                        )

        | _ -> false

    [<Extension>]
    static member IsTagForRedisActionOutput(x:TagEvent) =
        match x with
        | EventTaskDev (_, _, kind) ->
            kind.IsOneOf(
                TaskDevTag.actionOut
                , TaskDevTag.actionMemory
            )
        | _ -> false

    [<Extension>]
    static member IsVertexOriginTag(x:TagEvent) =
        match x with
        | EventVertex (_, _, kind) ->  kind.IsOneOf(   VertexTag.origin
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
    static member IsSystemErrTag(x:TagEvent) =
        match x with
        | EventSystem (_, _, kind) ->
            kind.IsOneOf(
                 SystemTag.emergencyMonitor
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
    static member IsActionOutTag(x:IStorage) =
        x.TagKind.IsOneOf(
                int TaskDevTag.actionMemory
              , int TaskDevTag.actionOut
              )



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
                      TaskDevTag.planStart
                    , TaskDevTag.planEnd
                    , TaskDevTag.actionIn
                    , TaskDevTag.actionOut
                    , TaskDevTag.actionMemory
                    )
            | EventHwSys (_, _, _) -> false
            | EventVariable (_, _, _) -> true
            | EventJob (_, _, _) -> true
