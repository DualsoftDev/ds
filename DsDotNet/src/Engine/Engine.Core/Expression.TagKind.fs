namespace Engine.Core

open Dual.Common.Core.FS
open System
open System.Runtime.CompilerServices
open System.Reactive.Subjects

[<AutoOpen>]
module TagKindModule =

    let [<Literal>] TagStartSystem       = 0
    let [<Literal>] TagStartFlow         = 10000
    let [<Literal>] TagStartVertex       = 11000
    let [<Literal>] TagStartApi          = 12000
    let [<Literal>] TagStartAction       = 14000
    let [<Literal>] TagStartActionHwTag  = 15000

    
    let InnerTag = -1

    [<Flags>]
    /// 0 ~ 9999
    type SystemTag  =
    | on                       = 0000
    | off                      = 0001
    | auto_btn                 = 0002
    | manual_btn               = 0003
    | drive_btn                = 0004
    | stop_btn                 = 0005
    | emg_btn                  = 0006
    | test_btn                 = 0007
    | ready_btn                = 0008
    | clear_btn                = 0009
    | home_btn                 = 0010

    | auto_lamp                = 0012
    | manual_lamp              = 0013
    | drive_lamp               = 0014
    | stop_lamp                = 0015
    | emg_lamp                 = 0016
    | test_lamp                = 0017
    | ready_lamp               = 0018
    | clear_lamp               = 0019
    | home_lamp                = 0020
    ///sysdatatimetag
    | datet_yy                 = 0021
    | datet_mm                 = 0022
    | datet_dd                 = 0023
    | datet_h                  = 0024
    | datet_m                  = 0025
    | datet_s                  = 0026
    ///systxErrTimetag             
    | timeout                  = 0027
    ///stopType
    | sysStopError             = 0030
    | sysStopPause             = 0031
    | sysDrive                 = 0032

    

 
    ///flicker
    | flicker200ms              = 0100
    | flicker1s                 = 0101
    | flicker2s                 = 0102

    ///simulation
    | sim                      = 9999
    /// 10000 ~ 10999
    [<Flags>]
    type FlowTag    =        
    |ready_mode                = 10000
    |auto_mode                 = 10001
    |manual_mode               = 10002
    |drive_mode                = 10003
    |test_mode                 = 10004
    |stop_mode                 = 10005
    |emg_mode                  = 10006
    |idle_mode                 = 10007

    |auto_btn                  = 10011
    |manual_btn                = 10012
    |drive_btn                 = 10013
    |stop_btn                  = 10014
    |ready_btn                 = 10015
    |clear_btn                 = 10016
    |emg_btn                   = 10017
    |test_btn                  = 10018
    |home_btn                  = 10019

    |auto_lamp                 = 10021
    |manual_lamp               = 10022
    |drive_lamp                = 10023
    |stop_lamp                 = 10024
    |ready_lamp                = 10025
    |clear_lamp                = 10026
    |emg_lamp                  = 10027
    |test_lamp                 = 10028
    |home_lamp                 = 10029
    
    ///stopType
    | flowStopError                = 10030
    | flowStopPause                = 10031

    /// 11000 ~ 11999
    [<Flags>]
    type VertexTag  =
    |startTag                  = 11000
    |resetTag                  = 11001
    |endTag                    = 11002
    
    //|spare                   = 11003
    //|spare                   = 11004
    //|spare                   = 11005

    |ready                     = 11006
    |going                     = 11007
    |finish                    = 11008
    |homing                    = 11009
    |origin                    = 11010
    |pause                     = 11011
    |errorTx                   = 11012
    |errorRx                   = 11013
    |errorTRx                  = 11014
    |realOriginAction          = 11016
    |relayReal                 = 11017

    |forceStart                = 11018
    |forceReset                = 11019
    |forceOn                   = 11020
    |forceOff                  = 11021

    |counter                   = 11023
    |timerOnDelay              = 11024
    |goingRealy                = 11025


    /// 12000 ~ 12999
    [<Flags>]
    type ApiItemTag =
    |planSet                   = 12000
    //|planRst                   = 12001  //not use
    |planEnd                   = 12002
    |txErrTrendOut             = 12003
    |txErrTimeOver             = 12004
    |rxErrShort                = 12005
    |rxErrShortOn              = 12006
    |rxErrShortRising          = 12007
    |rxErrShortTemp            = 12008
    |rxErrOpen                 = 12009
    |rxErrOpenOff              = 12010
    |rxErrOpenRising           = 12011
    |rxErrOpenTemp             = 12012
    |trxErr                    = 12013

    

    /// 13000 ~ 13999
    [<Flags>]
    type LinkTag    =
    |LinkStart                 = 13000
    |LintReset                 = 13001

    /// 14000 ~ 14999
    [<Flags>]
    type ActionTag    =
    |ActionIn                 = 14000
    |ActionOut                = 14001
    |ActionMemory             = 14002

    /// 15000 ~ 14999
    [<Flags>]
    type HwSysTag    =
    |HwSysIn                     = 15000
    |HwSysOut                    = 15001


    type TagDS =
        | EventSystem   of Tag: IStorage * Target: DsSystem     * TagKind: SystemTag
        | EventFlow     of Tag: IStorage * Target: Flow         * TagKind: FlowTag
        | EventVertex   of Tag: IStorage * Target: Vertex       * TagKind: VertexTag
        | EventApiItem  of Tag: IStorage * Target: ApiItem      * TagKind: ApiItemTag
        | EventAction   of Tag: IStorage * Target: DsTask       * TagKind: ActionTag
        | EventHwSys    of Tag: IStorage * Target: HwSystemItem * TagKind: HwSysTag


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
    [<Extension>] static member GetSystemTagKind   (x:IStorage) = DU.tryGetEnumValue<SystemTag>(x.TagKind)
    [<Extension>] static member GetFlowTagKind     (x:IStorage) = DU.tryGetEnumValue<FlowTag>(x.TagKind)
    [<Extension>] static member GetVertexTagKind   (x:IStorage) = DU.tryGetEnumValue<VertexTag>(x.TagKind)
    [<Extension>] static member GetApiTagKind      (x:IStorage) = DU.tryGetEnumValue<ApiItemTag>(x.TagKind)
    [<Extension>] static member GetActionTagKind   (x:IStorage) = DU.tryGetEnumValue<ActionTag>(x.TagKind)
    [<Extension>] static member GetHwSysTagTagKind (x:IStorage) = DU.tryGetEnumValue<HwSysTag>(x.TagKind)
    [<Extension>] static member GetAllTagKinds () : TagKindTuple array =
                    EnumEx.Extract<SystemTag>()
                    @ EnumEx.Extract<FlowTag>()
                    @ EnumEx.Extract<VertexTag>()
                    @ EnumEx.Extract<ApiItemTag>()
                    @ EnumEx.Extract<ActionTag>()

    [<Extension>]
    static member GetTagInfo (x:IStorage) =
        match x.Target with
        |Some obj ->
            match obj with
            | :? DsSystem as s     ->Some( EventSystem  (x, s, x.GetSystemTagKind().Value))
            | :? Flow as f         ->Some( EventFlow    (x, f, x.GetFlowTagKind().Value))
            | :? Vertex as v       ->Some( EventVertex  (x, v, x.GetVertexTagKind().Value))
            | :? ApiItem as a      ->Some( EventApiItem (x, a, x.GetApiTagKind().Value))
            | :? DsTask  as d      ->Some( EventAction  (x, d, x.GetActionTagKind().Value))
            | :? HwSystemItem as h ->Some( EventHwSys   (x, h, x.GetHwSysTagTagKind().Value))
            |_ -> None
        |None -> None
   
    [<Extension>]
    static member GetStorage(x:TagDS) =
        match x with
        |EventSystem (i, _, _) -> i
        |EventFlow   (i, _, _) -> i
        |EventVertex (i, _, _) -> i       
        |EventApiItem(i, _, _) -> i
        |EventAction (i, _, _) -> i
        |EventHwSys  (i, _, _) -> i




    [<Extension>]
    static member GetTarget(x:TagDS) =
        match x with
        |EventSystem ( _, target, _) -> target |> box
        |EventFlow   ( _, target, _) -> target |> box
        |EventVertex ( _, target, _) -> target |> box
        |EventApiItem( _, target, _) -> target |> box
        |EventAction ( _, target, _) -> target |> box
        |EventHwSys  ( _, target, _) -> target |> box

      
    [<Extension>]
    static member GetTagToText(x:TagDS) =
        let getText(tag:IStorage) (obj:INamed) kind = $"{tag.Name};{tag.BoxedValue};{obj.Name};{kind}"
        match x with
        |EventSystem (tag, obj, kind) -> getText tag obj kind
        |EventFlow   (tag, obj, kind) -> getText tag obj kind
        |EventVertex (tag, obj, kind) -> getText tag obj kind
        |EventApiItem(tag, obj, kind) -> getText tag obj kind
        |EventAction (tag, obj, kind) -> getText tag obj kind
        |EventHwSys  (tag, obj, kind) -> getText tag obj kind
        
    

    [<Extension>]
    static member GetSystem(x:TagDS) =
        match x with
        |EventSystem (_, obj, _) -> obj
        |EventFlow   (_, obj, _) -> obj.System
        |EventVertex (_, obj, _) -> obj.Parent.GetSystem()       
        |EventApiItem(_, obj, _) -> obj.System
        |EventAction (_, obj, _) -> obj.ApiItem.System
        |EventHwSys  (_, obj, _) -> obj.System
        
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
    static member IsVertexErrTag(x:TagDS) =
        match x with
        |EventVertex (_, _, kind) ->  kind.IsOneOf(  VertexTag.errorTx
                                                    , VertexTag.errorRx)
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
        |EventSystem (_, _, kind) ->  kind.IsOneOf(  SystemTag.sysDrive  
                                                    , SystemTag.sysStopPause
                                                    , SystemTag.sysStopError
                                                    , SystemTag.clear_btn)

        |EventFlow   (_, _, kind) ->  kind.IsOneOf(  FlowTag.drive_mode
                                                    , FlowTag.flowStopPause
                                                    , FlowTag.flowStopError
                                                    )

        |EventVertex (_, _, kind) ->  kind.IsOneOf(
                                        //VertexTag.ready,
                                        VertexTag.going       
                                        //, VertexTag.finish       
                                        //, VertexTag.homing       
                                        , VertexTag.pause       
                                        , VertexTag.errorTRx       
                                        , VertexTag.errorRx       
                                        , VertexTag.errorTx)
                                          
        |EventApiItem(_, _, kind) ->  kind = ApiItemTag.trxErr
                                        || kind = ApiItemTag.rxErrOpen  
                                        || kind = ApiItemTag.rxErrShort  
                                        || kind = ApiItemTag.txErrTimeOver  
                                        || kind = ApiItemTag.txErrTrendOut  
        |EventAction (_, _, _) -> false
        |EventHwSys  (_, _, _) -> false
