namespace Engine.CodeGenCPU

open System

[<AutoOpen>]
module CodeSpecUtil =

    [<AutoOpen>]
    type SREType =
        | Start
        | Reset
        | End

    [<Flags>]
    [<AutoOpen>]
    type ConvertType =
        | RealInFlow          = 0b0000000001
        | RealExFlow          = 0b0000000010
        | RealExSystem        = 0b0000000100
        | CallInFlow          = 0b0000001000
        | CallInReal          = 0b0000010000
        | AliasCallInReal     = 0b0000100000
        | AliasCallInFlow     = 0b0001000000
        | AliasRealInFlow     = 0b0010000000
        | AliasRealExInFlow   = 0b0100000000
        | AliasRealExInSystem = 0b1000000000
    // 고쳐야 함
    let InFlowWithoutReal = AliasRealExInFlow ||| AliasRealInFlow ||| AliasCallInFlow                                    ||| CallInFlow ||| RealExFlow                    // 0b11100110
    let InFlowAll         = AliasRealExInFlow ||| AliasRealInFlow ||| AliasCallInFlow                                    ||| CallInFlow ||| RealExFlow ||| RealInFlow     // 0b11100111
    let CoinTypeAll       = AliasRealExInFlow ||| AliasRealInFlow ||| AliasCallInFlow ||| AliasCallInReal ||| CallInReal ||| CallInFlow ||| RealExFlow                    // 0b11111110
    let CallTypeAll       =                                                                                   CallInReal ||| CallInFlow                                   // 0b00001100
    let RealNIndirectReal = AliasRealExInFlow ||| AliasRealInFlow                                                                       ||| RealExFlow ||| RealInFlow     // 0b11000011
    let VertexAll         = AliasRealExInFlow ||| AliasRealInFlow ||| AliasCallInFlow ||| AliasCallInReal ||| CallInReal ||| CallInFlow ||| RealExFlow ||| RealInFlow     // 0b11111111


