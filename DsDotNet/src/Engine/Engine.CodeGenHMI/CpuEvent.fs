// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.CodeGenHMI

open System
open System.Reactive.Subjects
open Engine.Core



[<AutoOpen>]
module CpuEvent = 
    
    type BitParam = |BIT of Bit:IBit * Value:bool

    let BitSubject = new Subject<BitParam>()
  
    let ChangeStatus (bit, value) = 
        async {
            BitSubject.OnNext(BitParam.BIT (bit, value))
        } |> Async.StartImmediate 
        
