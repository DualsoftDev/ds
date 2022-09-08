// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Runtime.CompilerServices
open System
open System.Reactive.Subjects



[<AutoOpen>]
module Event = 
    
    type MSGLevel = |Info | Warn | Error
    type MSGParam = |MSG of Time:DateTime * Level:MSGLevel * Message:string
    type SegParam = |SEG of Time:DateTime * Segment:SegmentBase * Status:Status
    type ProParam = |PRO of Time:DateTime * pro:int

    let MSGSubject = new Subject<MSGParam>()
    let SegSubject = new Subject<SegParam>()
    let ProcessSubject = new Subject<ProParam>()
    /// Message 공지.
    let MSGInfo (text:string)  = MSGSubject.OnNext(MSGParam.MSG (DateTime.Now, Info, text))
    let MSGWarn (text:string)  = MSGSubject.OnNext(MSGParam.MSG (DateTime.Now, Warn, text))
    let MSGError (text:string) = MSGSubject.OnNext(MSGParam.MSG (DateTime.Now, Error, text))
    let DoWork  (pro:int) = ProcessSubject.OnNext(ProParam.PRO (DateTime.Now, pro))
    let ChangeStatus (seg:SegmentBase, status:Status) = 
        async {
            SegSubject.OnNext(SegParam.SEG (DateTime.Now, seg, status))
        } |> Async.StartImmediate
        
      

    [<Extension>]
    type DsUtil =

        [<Extension>] static member GetTgtSame (edges:#EdgeBase seq, target) = edges  |> Seq.filter (fun edge -> edge.TargetVertex = target)  
        [<Extension>] static member GetSrcSame (edges:#EdgeBase seq, source) = edges  |> Seq.filter (fun edge -> edge.SourceVertex = source)  
        [<Extension>] static member GetStartCaual     (edges:#EdgeBase seq)  = edges |> Seq.filter (fun edge -> edge.Causal = SEdge)  
        [<Extension>] static member GetResetCaual     (edges:#EdgeBase seq)  = edges |> Seq.filter (fun edge -> edge.Causal = REdge)  
        [<Extension>] static member GetNodes          (edges:#EdgeBase seq)  = edges |> Seq.collect (fun edge -> [edge.SourceVertex;edge.TargetVertex])
        [<Extension>] static member GetNodesDistinct  (edges:#EdgeBase seq)  = edges.GetNodes() |> Seq.distinct 
