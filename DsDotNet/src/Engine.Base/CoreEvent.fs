// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System
open System.Reactive.Subjects



[<AutoOpen>]
module CoreEvent = 
    
    type SegParam = |SEG of Time:DateTime * Seg:SegBase * Status4:Status4

    let SegSubject = new Subject<SegParam>()
    /// Message 공지.
  
    let ChangeStatus (seg:SegBase, status:Status4) = 
        async {
            SegSubject.OnNext(SegParam.SEG (DateTime.Now, seg, status))
        } |> Async.StartImmediate 
        
