namespace Engine.Cpu

open System.Collections.Concurrent

[<AutoOpen>]
module Interface =
    
    type ICpuBit = 
        abstract Value: bool with get, set

    type IGate = 
        abstract Update: unit -> unit
        abstract AddBit:    ConcurrentDictionary<ICpuBit, bool>*ICpuBit*bool -> unit
        abstract RemoveBit: ConcurrentDictionary<ICpuBit, bool>*ICpuBit      -> unit

    type IBitReadable = inherit ICpuBit
    type IBitWritable = 
        inherit ICpuBit
        abstract SetValue: bool -> unit
    type IBitReadWritable = 
        inherit IBitReadable
        inherit IBitWritable

