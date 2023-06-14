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
        StatusSubject.OnNext(VertexStatusParam.Event (sys, vertex, status))
    //for UI
    let onValueChanged(sys:ISystem, stg:IStorage, v:obj) =
        ValueSubject.OnNext(ValueChangeParam.Event(sys, stg, v))

