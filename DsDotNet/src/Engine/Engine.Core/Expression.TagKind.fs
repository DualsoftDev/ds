namespace Engine.Core

open Dual.Common.Core.FS
open System
open System.Runtime.CompilerServices
open System.Reactive.Subjects

[<AutoOpen>]
module TagKindModule =

    let [<Literal>] TagStartSystem  = 0
    let [<Literal>] TagStartFlow    = 10000
    let [<Literal>] TagStartVertex  = 11000
    let [<Literal>] TagStartApi     = 12000
    let [<Literal>] TagStartAction  = 14000
    let InnerTag = -1

    [<Flags>]
    /// 0 ~ 9999
    type SystemTag  =
    | on                       = 0000
    | off                      = 0001
    | auto                     = 0002
    | manual                   = 0003
    | drive                    = 0004
    | stop                     = 0005
    | emg                      = 0006
    | test                     = 0007
    | ready                    = 0008
    | clear                    = 0009
    | home                     = 0010
    ///sysdatatimetag
    | datet_yy                 = 0011
    | datet_mm                 = 0012
    | datet_dd                 = 0013
    | datet_h                  = 0014
    | datet_m                  = 0015
    | datet_s                  = 0016
    ///systxErrTimetag
    | timeout                  = 0017
    ///stopType
    | sysError                 = 0020
    | sysPause                 = 0021
    | sysDrive                 = 0022

    

    ///simulation
    | sim                      = 9999

    /// 10000 ~ 10999
    [<Flags>]
    type FlowTag    =
    |ready_op                  = 10000
    |auto_op                   = 10001
    |manual_op                 = 10002
    |drive_op                  = 10003
    |test_op                   = 10004
    |stop_op                   = 10005
    |emergency_op              = 10006
    |idle_op                   = 10007
    |auto_bit                  = 10008
    |manual_bit                = 10009
    |drive_bit                 = 10010
    |stop_bit                  = 10011
    |ready_bit                 = 10012
    |clear_bit                 = 10013
    |emg_bit                   = 10014
    |test_bit                  = 10015
    |home_bit                  = 10016
    
    ///stopType
    | flowError                = 10020
    | flowPause                = 10021

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
    |realOriginAction          = 11016
    |relayReal                 = 11017

    |forceStart                = 11018
    |forceReset                = 11019
    |forceOn                   = 11020
    |forceOff                  = 11021

    |relayCall                 = 11022
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

    type TagDS =
        | EventSystem  of Tag: IStorage * Target: DsSystem * TagKind: SystemTag
        | EventFlow    of Tag: IStorage * Target: Flow     * TagKind: FlowTag
        | EventVertex  of Tag: IStorage * Target: Vertex   * TagKind: VertexTag
        | EventApiItem of Tag: IStorage * Target: ApiItem  * TagKind: ApiItemTag
        | EventAction  of Tag: IStorage * Target: DsTask   * TagKind: ActionTag


    let TagDSSubject = new Subject<TagDS>()

    type TagWeb = {
        Name  : string //FQDN 고유이름
        _SerializedObject : string
        Kind  : int    //Tag 종류 ex) going = 11007
        Message : string //에러 내용 및 기타 전달 Message 
    }
    type TagWeb with
        member x.Value:obj = ObjectHolder.Deserialize(x._SerializedObject).GetValue()

[<AutoOpen>]
[<Extension>]
type TagKindExt =
    [<Extension>] static member OnChanged (tagDS:TagDS) = TagDSSubject.OnNext(tagDS)
    [<Extension>] static member GetSystemTagKind (x:IStorage) = DU.tryGetEnumValue<SystemTag>(x.TagKind)
    [<Extension>] static member GetFlowTagKind   (x:IStorage) = DU.tryGetEnumValue<FlowTag>(x.TagKind)
    [<Extension>] static member GetVertexTagKind (x:IStorage) = DU.tryGetEnumValue<VertexTag>(x.TagKind)
    [<Extension>] static member GetApiTagKind    (x:IStorage) = DU.tryGetEnumValue<ApiItemTag>(x.TagKind)
    [<Extension>] static member GetActionTagKind (x:IStorage) = DU.tryGetEnumValue<ActionTag>(x.TagKind)
    [<Extension>] static member GetValue (x:TagWeb) : obj = x.Value

    [<Extension>]
    static member GetTagInfo (x:IStorage) =
        match x.Target with
        |Some obj ->
            match obj with
            | :? DsSystem as s ->Some( EventSystem  (x, s, x.GetSystemTagKind().Value))
            | :? Flow as f     ->Some( EventFlow    (x, f, x.GetFlowTagKind().Value))
            | :? Vertex as v   ->Some( EventVertex  (x, v, x.GetVertexTagKind().Value))
            | :? ApiItem as a  ->Some( EventApiItem (x, a, x.GetApiTagKind().Value))
            | :? DsTask  as d  ->Some( EventAction  (x, d, x.GetActionTagKind().Value))
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




    [<Extension>]
    static member GetTarget(x:TagDS) =
        match x with
        |EventSystem ( _, target, _) -> target |> box
        |EventFlow   ( _, target, _) -> target |> box
        |EventVertex ( _, target, _) -> target |> box
        |EventApiItem( _, target, _) -> target |> box
        |EventAction ( _, target, _) -> target |> box

      
    [<Extension>]
    static member GetTagToText(x:TagDS) =
        let getText(tag:IStorage) (obj:INamed) kind = $"{tag.Name};{tag.BoxedValue};{obj.Name};{kind}"
        match x with
        |EventSystem (tag, obj, kind) -> getText tag obj kind
        |EventFlow   (tag, obj, kind) -> getText tag obj kind
        |EventVertex (tag, obj, kind) -> getText tag obj kind
        |EventApiItem(tag, obj, kind) -> getText tag obj kind
        |EventAction (tag, obj, kind) -> getText tag obj kind
        
    [<Extension>]
    static member GetWebTag(x:TagDS) : TagWeb =
        let createTagWeb (tag:IStorage) (obj:IQualifiedNamed) =
            { Name = obj.QualifiedName; _SerializedObject = ObjectHolder.Create(tag.BoxedValue).Serialize(); Kind = tag.TagKind; Message = ""}
        match x with
        | EventSystem (tag, obj, _) -> createTagWeb tag obj
        | EventFlow   (tag, obj, _) -> createTagWeb tag obj
        | EventVertex (tag, obj, _) -> createTagWeb tag obj
        | EventApiItem(tag, obj, _) -> createTagWeb tag obj
        | EventAction (tag, obj, _) -> createTagWeb tag obj

    [<Extension>] static member SetMessage(x:TagWeb, messsage) : TagWeb = {x with Message=messsage}

    [<Extension>]
    static member GetSystem(x:TagDS) =
        match x with
        |EventSystem (_, obj, _) -> obj
        |EventFlow   (_, obj, _) -> obj.System
        |EventVertex (_, obj, _) -> obj.Parent.GetSystem()       
        |EventApiItem(_, obj, _) -> obj.System
        |EventAction (_, obj, _) -> obj.ApiItem.System
        
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
                                                    , SystemTag.sysError
                                                    , SystemTag.clear)

        |EventFlow   (_, _, kind) ->  kind.IsOneOf(  FlowTag.drive_op
                                                    , FlowTag.flowError)

        |EventVertex (_, _, kind) ->  kind.IsOneOf(
                                        //VertexTag.ready,
                                        VertexTag.going       
                                        //, VertexTag.finish       
                                        //, VertexTag.homing       
                                        , VertexTag.errorRx       
                                        , VertexTag.errorTx)
                                          
        |EventApiItem(_, _, kind) ->  kind = ApiItemTag.trxErr
                                        || kind = ApiItemTag.rxErrOpen  
                                        || kind = ApiItemTag.rxErrShort  
                                        || kind = ApiItemTag.txErrTimeOver  
                                        || kind = ApiItemTag.txErrTrendOut  
        |EventAction (_, _, _) -> false
