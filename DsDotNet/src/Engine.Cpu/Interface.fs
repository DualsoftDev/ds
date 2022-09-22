namespace Engine.Cpu
open Engine.Core

[<AutoOpen>]
module Interface =

    type IAlias     = inherit INamed
    type ITxRx      = inherit INamed

    type IAutoTag   = inherit IBit

    type IWeakEdge   = inherit IEdge
    type ISetEdge    = inherit IEdge
    type IResetEdge  = inherit IEdge
    type IStrongEdge = inherit IEdge

    type IEngine    = inherit INamed

    type IBitReadable = inherit IBit
    type IBitWritable = 
        inherit IBit
        abstract SetValue:bool;
    type IBitReadWritable = 
        inherit IBitReadable
        inherit IBitWritable

    type ICoin      = inherit IVertex
    type IWallet    = inherit IVertex
