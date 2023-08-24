namespace Engine.Core

open Dual.Common.Core.FS
open System
open System.Runtime.CompilerServices

[<AutoOpen>]
module TagKindModule =

    let [<Literal>] TagStartSystem  = 0
    let [<Literal>] TagStartFlow    = 10000
    let [<Literal>] TagStartVertex  = 11000
    let [<Literal>] TagStartApi     = 12000
    let [<Literal>] TagStartAction  = 14000

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
    ///syserrortag
    | timeout                  = 0017
    ///simulation
    | sim                      = 9999

    [<Flags>]
    /// 10000 ~ 10999
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
    |readycondi_bit            = 10017
    |drivecondi_bit            = 10018


    [<Flags>]
    /// 11000 ~ 11999
    type VertexTag  =
    |startTag                  = 11000
    |resetTag                  = 11001
    |endTag                    = 11002
    |startForce                = 11003
    |resetForce                = 11004
    |endForce                  = 11005
    |ready                     = 11006
    |going                     = 11007
    |finish                    = 11008
    |homing                    = 11009
    |origin                    = 11010
    |pause                     = 11011
    |errorTx                   = 11012
    |errorRx                   = 11013
    |goingRelayGroup           = 11014
    |goingrelay                = 11015
    |realOriginAction          = 11016
    |relayReal                 = 11017
    |startPort                 = 11018
    |resetPort                 = 11019
    |endPort                   = 11020
    |relayCall                 = 11021
    |counter                   = 11022
    |timerOnDelay              = 11023
    |timerTimeOut              = 11024


    [<Flags>]
    /// 12000 ~ 12999
    type ApiItemTag =
    |planSet                   = 12000
    |planRst                   = 12001
    |planEnd                   = 12002

    [<Flags>]
    /// 13000 ~ 13999
    type LinkTag    =
    |LinkStart                 = 13000
    |LintReset                 = 13001

    [<Flags>]
    /// 14000 ~ 14999
    type ActionTag    =
    |ActionIn                 = 14000
    |ActionOut                = 14001
    |ActionMemory             = 14002




    [<AutoOpen>]
    type TagTargetDS =
    | TTSystem  of FqdnObject * SystemTag
    | TTFlow    of FqdnObject * FlowTag
    | TTVertex  of FqdnObject * VertexTag
    | TTApiItem of FqdnObject * ApiItemTag
    | TTAction  of FqdnObject * ActionTag


    [<AutoOpen>]
    [<Extension>]
    type TagKindExt =

        [<Extension>]
        static member GetSystemTagKind (x:IStorage) = match x.TagKind with | InClosedRange TagStartSystem  9999 -> Some (Enum.ToObject(typeof<SystemTag>, x.TagKind) :?> SystemTag)  | _ -> None
        [<Extension>]
        static member GetFlowTagKind   (x:IStorage) = match x.TagKind with | InClosedRange TagStartFlow   10999 -> Some (Enum.ToObject(typeof<FlowTag>, x.TagKind) :?> FlowTag)      | _ -> None
        [<Extension>]
        static member GetVertexTagKind (x:IStorage) = match x.TagKind with | InClosedRange TagStartVertex 11999 -> Some (Enum.ToObject(typeof<VertexTag>, x.TagKind) :?> VertexTag)  | _ -> None
        [<Extension>]
        static member GetApiTagKind    (x:IStorage) = match x.TagKind with | InClosedRange TagStartApi    12999 -> Some (Enum.ToObject(typeof<ApiItemTag>, x.TagKind) :?> ApiItemTag)| _ -> None
        [<Extension>]
        static member GetActionTagKind (x:IStorage) = match x.TagKind with | InClosedRange TagStartAction 14999 -> Some (Enum.ToObject(typeof<ActionTag>, x.TagKind) :?> ActionTag)| _ -> None

        [<Extension>]
        static member GetTagInfo (x:IStorage) =
            match x.Target with
            |Some obj ->
                match obj with
                | :? DsSystem as s ->Some( TTSystem (s,  x.GetSystemTagKind().Value))
                | :? Flow as f     ->Some( TTFlow (f,  x.GetFlowTagKind().Value))
                | :? Vertex as v   ->Some( TTVertex (v,  x.GetVertexTagKind().Value))
                | :? ApiItem as a  ->Some( TTApiItem (a,  x.GetApiTagKind().Value))
                | :? DsTask  as d  ->Some( TTAction (d,  x.GetActionTagKind().Value))
                |_ -> None
            |None -> None

        [<Extension>]
        static member GetVertexTagKindText (x:IStorage) =
            let info = x.GetTagInfo()
            match info with
            |Some t -> 
                match t with
                |TTSystem (_,tag) -> tag.ToString()
                |TTFlow (_,tag) -> tag.ToString()
                |TTVertex (_,tag) -> tag.ToString()
                |TTApiItem (_,tag) -> tag.ToString()
                |TTAction (_,tag) -> tag.ToString()
            |None -> "None"
            