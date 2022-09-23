// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Cpu

open System
open System.Reactive.Subjects
open Engine.Core



[<AutoOpen>]
module CpuEvent = 
    
    type BitParam = |BIT of Time:DateTime * Bit:IBit * Value:bool

    let SegSubject = new Subject<BitParam>()
    /// Message 공지.
  
    let ChangeStatus (seg:SegBase, bit, value) = 
        async {
            SegSubject.OnNext(BitParam.BIT (DateTime.Now, bit, value))
        } |> Async.StartImmediate 
        
