namespace Engine.CpuUnit

open System.Diagnostics
open System.Collections.Generic
open Engine.Core

[<AutoOpen>]
module CpuUnit =

  
    [<DebuggerDisplay("{name}")>]
    type Cpu(name:string, model:Model)  =
        let rootFlows = HashSet<IFlow>() 
        interface ICpu 

        member x.Name = name
        member x.RootFlows = rootFlows
        member x.Model = model
        member val IsActive = false with get, set
        member val Running = false with get, set
