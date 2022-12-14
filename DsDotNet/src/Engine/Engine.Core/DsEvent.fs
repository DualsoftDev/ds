// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System
open System.Reactive.Subjects


[<AutoOpen>]
module CpuEvent = 
    
    type VertexStatusParam = 
                |Event of vertex:IVertex * status:Status4

    let TypedValueSubject = new Subject<IStorage>()
    let Status4Subject = new Subject<VertexStatusParam>()
    
    let event(param:IStorage) =
             async {
                TypedValueSubject.OnNext(param)
             } |> Async.StartImmediate 

    let ChangeValueEvent(storage:IStorage) = event storage
    let ChangeStatusEvent(vertex:IVertex, status:Status4) =  
        Status4Subject.OnNext(VertexStatusParam.Event (vertex, status))
        
