// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System
open System.Reactive.Subjects


[<AutoOpen>]
module CpuEvent =

    type VertexStatusParam =
                |Event of sys:ISystem * vertex:IVertex * status:Status4
    type ValueChangeParam =
                |Event of sys:ISystem * storage:IStorage * value:obj

    let StatusSubject = new Subject<VertexStatusParam>()
    let ValueSubject = new Subject<ValueChangeParam>()

    //for UI
    let onStatusChanged(sys:ISystem, vertex:IVertex, status:Status4) =
        async { StatusSubject.OnNext(VertexStatusParam.Event (sys, vertex, status)) }
        |> Async.RunSynchronously
    //for UI
    let onValueChanged(sys:ISystem, stg:IStorage, v:obj) =
        async { ValueSubject.OnNext(ValueChangeParam.Event(sys, stg, v)) }
        |> Async.RunSynchronously

