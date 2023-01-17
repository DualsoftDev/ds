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
        | RealInFlow          = 0b00000001
        | RealExFlow          = 0b00000010
        | CallInFlow          = 0b00000100
        | CallInReal          = 0b00001000
        | AliasCallInReal     = 0b00010000
        | AliasCallInFlow     = 0b00100000
        | AliasRealInFlow     = 0b01000000
        | AliasRealExInFlow   = 0b10000000

    let InFlowWithoutReal = AliasRealExInFlow ||| AliasRealInFlow ||| AliasCallInFlow                                    ||| CallInFlow ||| RealExFlow                    // 0b11100110
    let InFlowAll         = AliasRealExInFlow ||| AliasRealInFlow ||| AliasCallInFlow                                    ||| CallInFlow ||| RealExFlow ||| RealInFlow     // 0b11100111
    let CoinTypeAll       = AliasRealExInFlow ||| AliasRealInFlow ||| AliasCallInFlow ||| AliasCallInReal ||| CallInReal ||| CallInFlow ||| RealExFlow                    // 0b11111110
    let CallTypeAll       =                                                                                   CallInReal ||| CallInFlow                                   // 0b00001100
    let RealNIndirectReal = AliasRealExInFlow ||| AliasRealInFlow                                                                       ||| RealExFlow ||| RealInFlow     // 0b11000011
    let VertexAll         = AliasRealExInFlow ||| AliasRealInFlow ||| AliasCallInFlow ||| AliasCallInReal ||| CallInReal ||| CallInFlow ||| RealExFlow ||| RealInFlow     // 0b11111111


