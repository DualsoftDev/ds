// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Runtime.CompilerServices
open System
open System.Globalization

[<AutoOpen>]
module Util = 
    
    [<Extension>]
    type EdgeUtil =

        [<Extension>] static member GetTgtSame (edges:#IEdge seq, target) = edges |> Seq.filter (fun edge -> edge.Target = target)  
        [<Extension>] static member GetSrcSame (edges:#IEdge seq, source) = edges |> Seq.filter (fun edge -> edge.Source = source)  
        [<Extension>] static member GetStartCaual     (edges:#IEdge seq)  = edges |> Seq.filter (fun edge -> edge.Causal.IsStart)  
        [<Extension>] static member GetResetCaual     (edges:#IEdge seq)  = edges |> Seq.filter (fun edge -> edge.Causal.IsReset)  
        [<Extension>] static member GetNodes   (edges:#IEdge seq)  = edges |> Seq.collect (fun edge -> [edge.Source;edge.Target])  
    