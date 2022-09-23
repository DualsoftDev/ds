namespace Engine.Cpu

open System.Diagnostics
open System.Collections.Generic
open Engine.Core

[<AutoOpen>]
module Cpu =

  
    [<DebuggerDisplay("{name}")>]
    type DsCpu(name:string)  =
        let mutable name = name
        let assignFlows = HashSet<IFlow>() 
        interface ICpu with
            member _.Name with get () = name and set (v) = name <- v

        member x.CpuName = (x:> ICpu).Name
        member x.AssignFlows = assignFlows
        member val IsActive = false with get, set
