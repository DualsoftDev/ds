// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Core

open System.Reactive.Subjects
open Engine.Common
open Dual.Common.Core.FS

[<AutoOpen>]
module CpusEvent =

    // Represents the status parameters for a Vertex.
    type VertexStatusParam =
        | EventCPU of sys: ISystem * vertex: IVertex * status: Status4

    // Subjects to broadcast status and value changes.
    let StatusSubject = new Subject<VertexStatusParam>()
    /// Represents (system, storage, value)
    let ValueSubject  = new Subject<ISystem * IStorage * obj>()

    // Notifies subscribers about a status change.
    let onStatusChanged(sys: ISystem, vertex: IVertex, status: Status4) =
        StatusSubject.OnNext(EventCPU (sys, vertex, status))

    // Notifies subscribers about a value change.
    let onValueChanged(sys: ISystem, stg: IStorage, value: obj) =
        ValueSubject.OnNext(sys, stg, value)
