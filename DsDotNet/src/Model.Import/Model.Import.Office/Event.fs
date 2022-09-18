// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Runtime.CompilerServices
open System
open System.Reactive.Subjects

open Engine.Core
open Engine.Core.DsType


[<AutoOpen>]
module Event = 
    
    type SegParam = |SEG of Time:DateTime * Seg:SegBase * Status4:Status4

    let SegSubject = new Subject<SegParam>()
    /// Message 공지.
  
    let ChangeStatus (seg:SegBase, status:Status4) = 
        async {
            SegSubject.OnNext(SegParam.SEG (DateTime.Now, seg, status))
        } |> Async.StartImmediate
        
    [<Extension>]
    type DsUtil =

        [<Extension>] static member GetTgtSame (edges:#CausalBase seq, target) = edges |> Seq.filter (fun edge -> edge.Target = target)  
        [<Extension>] static member GetSrcSame (edges:#CausalBase seq, source) = edges |> Seq.filter (fun edge -> edge.Source = source)  
        [<Extension>] static member GetStartCaual     (edges:#CausalBase seq)  = edges |> Seq.filter (fun edge -> edge.Causal = SEdge)  
        [<Extension>] static member GetResetCaual     (edges:#CausalBase seq)  = edges |> Seq.filter (fun edge -> edge.Causal = REdge)  
        [<Extension>] static member GetNodes          (edges:#CausalBase seq)  = edges |> Seq.collect (fun edge -> [edge.Source ;edge.Target])
        [<Extension>] static member GetNodesDistinct  (edges:#CausalBase seq)  = edges.GetNodes() |> Seq.distinct 
