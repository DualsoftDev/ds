// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Runtime.CompilerServices
open System
open System.Reactive.Subjects



[<AutoOpen>]
module Event = 
    
    type SegParam = |SEG of Time:DateTime * Seg:SegBase * Status:Status

    let SegSubject = new Subject<SegParam>()
    /// Message 공지.
  
    let ChangeStatus (seg:SegBase, status:Status) = 
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
