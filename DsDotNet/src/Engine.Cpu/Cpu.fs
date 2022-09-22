namespace Engine.Cpu

open System.Diagnostics
open System.Collections.Generic
open Engine.Core

[<AutoOpen>]
module Cpu =

  
    [<DebuggerDisplay("{name}")>]
    type DsCpu(name:string)  =
        let assignFlows = HashSet<IFlow>() 
        interface ICpu with
            member _.ToText(): string = name
            member _.Name = name

        member x.CpuName = (x:> ICpu).Name
        member x.AssignFlows = assignFlows
        member val IsActive = false with get, set
