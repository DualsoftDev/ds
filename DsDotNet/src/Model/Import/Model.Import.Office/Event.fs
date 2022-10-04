// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System
open System.Reactive.Subjects
open Engine.Core



[<AutoOpen>]
module CoreEvent = 
    
    type SegParam = |SEG of Time:DateTime * Seg:Segment * Status4:Status4

    let SegSubject = new Subject<SegParam>()
    /// Message 공지.
  
    let ChangeStatus (seg:Segment, status:Status4) = 
        async {
            SegSubject.OnNext(SegParam.SEG (DateTime.Now, seg, status))
        } |> Async.StartImmediate 
        
