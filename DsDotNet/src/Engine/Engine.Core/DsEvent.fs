// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System
open System.Reactive.Subjects


[<AutoOpen>]
module CpuEvent = 
    
    type TypedValueStorageParam = 
                |Event of name:string * Value:obj

    let TypedValueSubject = new Subject<TypedValueStorageParam>()
    let event(param:TypedValueStorageParam) =
             async {
                TypedValueSubject.OnNext(param)
             } |> Async.StartImmediate 

    let ChangeValueEvent(name, value) = event (TypedValueStorageParam.Event (name, value))
        
