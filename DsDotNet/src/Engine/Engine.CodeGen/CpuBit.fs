namespace Engine.CodeGen

open Engine.Core
open System.Diagnostics
open System.Collections.Generic

[<AutoOpen>]
module CpuBit =

    [<AbstractClass>]
    [<DebuggerDisplay("{ToText()}")>]
    type Bit(cpu:ICpu, name) as this = 
        inherit Named(name)
        let mutable _value:bool = false
        let usedGates = HashSet<IGate>()
        let changeBit() = 
            usedGates |> Seq.map(fun (gate: IGate) -> async { gate.Update()})
                            |> Async.Parallel   |> Async.StartAsTask    |> ignore
                
        override x.ToText() = name
        interface ICpuBit with
            member _.Value with get () = _value 
                            and set (v) = let changed = _value <> v
                                          _value <- v
                                          if (changed) then changeBit()
        member x.Cpu = cpu
        member val Value = (this :> ICpuBit).Value with get,set
        //this Bit 가 사용된 Gate 들
        member x.AddGate(gate:IGate) = usedGates.Add(gate)
        member x.RemoveGate(gate:IGate) = usedGates.Remove(gate)

  