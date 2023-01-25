// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System
open System.Reactive.Subjects


[<AutoOpen>]
module CpuEvent =

    type VertexStatusParam =
                |Event of vertex:IVertex * status:Status4

    let StatusSubject = new Subject<VertexStatusParam>()

    let onStatusChanged(vertex:IVertex, status:Status4) =
        StatusSubject.OnNext(VertexStatusParam.Event (vertex, status))

