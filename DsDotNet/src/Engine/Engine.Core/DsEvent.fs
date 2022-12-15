// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System
open System.Reactive.Subjects


[<AutoOpen>]
module CpuEvent = 
    
    type VertexStatusParam = 
                |Event of vertex:IVertex * status:Status4

    let ValueSubject = new Subject<IStorage>()
    let StatusSubject = new Subject<VertexStatusParam>()
    
    let ChangeValueEvent(storage:IStorage) =
        async {ValueSubject.OnNext(storage)}
        |> Async.StartImmediate 

    let ChangeStatusEvent(vertex:IVertex, status:Status4) =  
        async {StatusSubject.OnNext(VertexStatusParam.Event (vertex, status))} 
        |> Async.StartImmediate 
        
