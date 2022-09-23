namespace Engine.Cpu

open Engine.Core
open System.Diagnostics

[<AutoOpen>]
module CpuBit =

    [<AbstractClass>]
    [<DebuggerDisplay("{ToText()}")>]
    type Bit(cpu:ICpu, name) as this = 
        inherit Named(name)
        let mutable _value:bool = false

        do ()

        override x.ToText() = name


        interface ICpuBit with
            member _.Value with get () = _value and set (v) = _value <- v

        member x.Cpu = cpu
        member val Value = (this :> ICpuBit).Value with get,set

  