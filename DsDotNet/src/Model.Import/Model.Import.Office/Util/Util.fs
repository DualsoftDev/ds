// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System
open System.Linq
open System.Diagnostics
open System.Collections.Concurrent
open System.Runtime.CompilerServices

[<AutoOpen>]
module Util =

    type E =
        /// relay 변화를 Trace.WriteLine   
        [<Extension>] static member ConsolLogAction(text:string) = 
                        Trace.WriteLine ($"{DateTime.Now.Second}.{DateTime.Now.Millisecond} {text}") 

  
    /// ConcurrentDictionary 를 이용한 hash
    type ConcurrentHash<'T>() =
        inherit ConcurrentDictionary<'T, 'T>()
        member x.TryAdd(item:'T) = x.TryAdd(item, item)
