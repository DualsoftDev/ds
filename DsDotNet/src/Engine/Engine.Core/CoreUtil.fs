// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

//open System.Runtime.CompilerServices
//open System
//open System.Globalization

//[<AutoOpen>]
//module Util = 
    
//    [<Extension>]
//    type EdgeUtil =

//        [<Extension>] static member GetTgtSame (edges:#IEdge seq, target) = edges |> Seq.filter (fun edge -> edge.Target = target)  
//        [<Extension>] static member GetSrcSame (edges:#IEdge seq, source) = edges |> Seq.filter (fun edge -> edge.Source = source)  
//        [<Extension>] static member GetTgtSameReset (edges:#IEdge seq, target) = edges.GetTgtSame(target) |> Seq.filter (fun edge -> edge.Causal.IsReset)  
//        [<Extension>] static member GetSrcSameReset (edges:#IEdge seq, target) = edges.GetSrcSame(target) |> Seq.filter (fun edge -> edge.Causal.IsReset)  
//        [<Extension>] static member GetStartCaual     (edges:#IEdge seq)  = edges |> Seq.filter (fun edge -> edge.Causal.IsStart)  
//        [<Extension>] static member GetResetCaual     (edges:#IEdge seq)  = edges |> Seq.filter (fun edge -> edge.Causal.IsReset)  
//        [<Extension>] static member GetStartNodes     (edges:#IEdge seq)  = edges.GetStartCaual() |> Seq.map (fun edge -> edge.Source)  
//        [<Extension>] static member GetResetNodes     (edges:#IEdge seq)  = edges.GetResetCaual() |> Seq.map (fun edge -> edge.Target)  
//        [<Extension>] static member GetNodes   (edges:#IEdge seq)  = edges |> Seq.collect (fun edge -> [edge.Source;edge.Target])  
//        //    public static string Combine(this string[] nameComponents, string separator=".") =>
//        //string.Join(separator, nameComponents.Select(n => n.IsQuotationRequired() ? $"\"{n}\"" : n));
//        ///Start Edge 기준으로 다음 Vertex 들을 찾음
//        [<Extension>] static member GetNextNodes (edges:#IEdge seq, source) = 
//                                edges.GetSrcSame(source) 
//                                |> Seq.filter (fun edge -> edge.Causal.IsStart)  
//                                |> Seq.map    (fun edge -> edge.Target)  

//        ///Start Edge 기준으로 이전 Vertex 들을 찾음
//        [<Extension>] static member GetPrevNodes (edges:#IEdge seq, target) =
//                                edges.GetTgtSame(target) 
//                                |> Seq.filter (fun edge -> edge.Causal.IsStart)  
//                                |> Seq.map    (fun edge -> edge.Source) 


