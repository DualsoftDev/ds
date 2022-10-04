namespace Engine.CpuUnit
open Engine.Core

[<AutoOpen>]
module Interface =

    type ICpuBit = 
        inherit IBit
        inherit ICpu
        abstract Value: bool with get, set

    type IAlias     = inherit INamed
    type ITxRx      = inherit INamed

    type IAutoTag   = inherit ICpuBit


    type IEngine    = inherit INamed

    type IBitReadable = inherit ICpuBit
    type IBitWritable = 
        inherit ICpuBit
        abstract SetValue: bool -> unit
    type IBitReadWritable = 
        inherit IBitReadable
        inherit IBitWritable

    type ICoin      = inherit IVertex
    type IWallet    = inherit IVertex
