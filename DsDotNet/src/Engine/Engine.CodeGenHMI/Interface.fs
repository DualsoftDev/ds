namespace Engine.CodeGenHMI
open Engine.Core

[<AutoOpen>]
module Interface =


    type IAlias     = inherit INamed
    type ITxRx      = inherit INamed


    type IEngine    = inherit INamed

 
    type ICoin      = inherit IVertex
    type IWallet    = inherit IVertex
