namespace Engine.Core

open Engine.Common.FS
open System
open System.Runtime.CompilerServices

[<AutoOpen>]
module TagKindModule =

    [<Flags>]
    type SystemTag  =
    | on                       = 0
    | off                      = 1
    | auto                     = 2
    | manual                   = 3
    | drive                    = 4
    | stop                     = 5
    | emg                      = 6
    | test                     = 7
    | ready                    = 8
    | clear                    = 9
    | home                     = 10
    ///sysdatatimetag
    | datet_yy                 = 11
    | datet_mm                 = 12
    | datet_dd                 = 13
    | datet_h                  = 14
    | datet_m                  = 15
    | datet_s                  = 16
    ///syserrortag
    | timeout                  = 17

    [<Flags>]
    type FlowTag    =
    |ready_op                  = 1000
    |auto_op                   = 1001
    |manual_op                 = 1002
    |drive_op                  = 1003
    |test_op                   = 1004
    |stop_op                   = 1005
    |emergency_op              = 1006
    |idle_op                   = 1007
    |auto_bit                  = 1008
    |manual_bit                = 1009
    |drive_bit                 = 1010
    |stop_bit                  = 1011
    |ready_bit                 = 1012
    |clear_bit                 = 1013
    |emg_bit                   = 1014
    |test_bit                  = 1015
    |home_bit                  = 1016
    |readycondi_bit            = 1017
    |drivecondi_bit            = 1018


    [<Flags>]
    type VertexTag  =
    |startTag                  = 2000
    |resetTag                  = 2001
    |endTag                    = 2002
    |startForce                = 2003
    |resetForce                = 2004
    |endForce                  = 2005
    |ready                     = 2006
    |going                     = 2007
    |finish                    = 2008
    |homing                    = 2009
    |origin                    = 2010
    |pause                     = 2011
    |errorTx                   = 2012
    |errorRx                   = 2013
    |pulse                     = 2014
    |goingrelay                = 2015
    |realOriginAction          = 2016
    |relayReal                 = 2017
    |startPort                 = 2018
    |resetPort                 = 2019
    |endPort                   = 2020
    |relayCall                 = 2021
    |counter                   = 2022
    |timerOnDelay              = 2023
    |timerTimeOut              = 2024


    [<Flags>]
    type ApiItemTag =
    |planSet                   = 3000
    |planRst                   = 3001
    |planEnd                   = 3002

    [<Flags>]
    type LinkTag    =
    |LinkStart                 = 4000
    |LintReset                 = 4000




    [<AutoOpen>]
    type TagTarget =
    | TTSystem
    | TTFlow
    | TTVertex
    | TTApiItem

    [<AutoOpen>]
    type TagDsInfo = {
        Name: string
        TagTarget : TagTarget
        TagSystem    : (DsSystem * SystemTag ) option
        TagFlow      : (Flow     * FlowTag   ) option
        TagVertex    : (Vertex   * VertexTag ) option
        TagApiItem   : (ApiItem  * ApiItemTag) option
    }

    [<AutoOpen>]
    [<Extension>]
    type TagKinExt =

        [<Extension>]
        static member GetTagInfo (x:IStorage) =
            let toSysTag    (tagKind:int) = Enum.ToObject(typeof<SystemTag>,  tagKind) :?> SystemTag
            let toFlowTag   (tagKind:int) = Enum.ToObject(typeof<FlowTag>,    tagKind) :?> FlowTag
            let toVertexTag (tagKind:int) = Enum.ToObject(typeof<VertexTag>,  tagKind) :?> VertexTag
            let toApiTag    (tagKind:int) = Enum.ToObject(typeof<ApiItemTag>, tagKind) :?> ApiItemTag
            match x.Target with
            |Some obj ->
                match obj with
                | :? DsSystem as s -> Some {Name= x.Name; TagTarget= TTSystem;  TagSystem= Some(s, toSysTag x.TagKind); TagFlow = None;  TagVertex = None;   TagApiItem= None}
                | :? Flow as f     -> Some {Name= x.Name; TagTarget= TTFlow;    TagSystem= None; TagFlow = Some(f, toFlowTag x.TagKind); TagVertex = None;   TagApiItem= None}
                | :? Vertex as v   -> Some {Name= x.Name; TagTarget= TTVertex;  TagSystem= None; TagFlow = None; TagVertex = Some(v, toVertexTag x.TagKind); TagApiItem= None}
                | :? ApiItem as a  -> Some {Name= x.Name; TagTarget= TTApiItem; TagSystem= None; TagFlow = None; TagVertex = None; TagApiItem= Some(a, toApiTag x.TagKind)}
                |_ -> None

            |None -> None
