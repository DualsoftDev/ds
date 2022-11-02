namespace Engine.Cpu

open System.Diagnostics
open System.Collections.Generic

[<AutoOpen>]
module CpuBit =

    type Bit(name) = 
        let mutable _value:bool = false
        let usedGates = HashSet<IGate>()
        let changeBit() = 
            usedGates |> Seq.map(fun (gate: IGate) -> async { gate.Update()})
                            |> Async.Parallel |> Async.StartAsTask |> ignore
                
        member x.Name = name
        interface ICpuBit with
            member _.Value with get () = _value 
                            and set (v) = let changed = _value <> v
                                          _value <- v
                                          if (changed) then changeBit()

        //this Bit 가 사용된 Gate 들
        member x.AddGate(gate:IGate) = usedGates.Add(gate)
        member x.RemoveGate(gate:IGate) = usedGates.Remove(gate)

  