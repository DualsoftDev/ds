// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System
open System.Reactive.Subjects


[<AutoOpen>]
module CpusEvent =

    type VertexStatusParam =
                |Event of sys:ISystem * vertex:IVertex * status:Status4
    //type ValueChangeParam =
    //            |Event of sys:ISystem * storage:IStorage * value:obj

    let StatusSubject = new Subject<VertexStatusParam>()
    let ValueSubject  = new Subject<ISystem * IStorage * obj>()
    let ValueHWSubject  = new Subject<ISystem * IStorage * obj>()

    let onStatusChanged(sys:ISystem, vertex:IVertex, status:Status4) =
        StatusSubject.OnNext(VertexStatusParam.Event (sys, vertex, status))
    let onValueChanged(sys:ISystem, stg:IStorage, v:obj) =
        ValueSubject.OnNext(sys, stg, v)

    let onValueHWChanged(sys:ISystem, stg:IStorage, v:obj) =
        ValueHWSubject.OnNext(sys, stg, v)

[<AutoOpen>]
module DsProcessEvent =

    type ProParam = |PRO of Time:DateTime * pro:int

    let mutable CurrProcess:int = 0
    let ProcessSubject = new Subject<ProParam>()

    let DoWork  (pro:int) =
        CurrProcess <- pro
        ProcessSubject.OnNext(ProParam.PRO (DateTime.Now, pro))



