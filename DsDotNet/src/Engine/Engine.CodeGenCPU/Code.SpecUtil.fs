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

    let InFlowWithoutReal = AliasRealExInSystem ||| AliasRealExInFlow ||| AliasRealInFlow ||| AliasCallInFlow                                    ||| CallInFlow ||| RealExSystem ||| RealExFlow                    // 0b1111001110
    let InFlowAll         = AliasRealExInSystem ||| AliasRealExInFlow ||| AliasRealInFlow ||| AliasCallInFlow                                    ||| CallInFlow ||| RealExSystem ||| RealExFlow ||| RealInFlow     // 0b1111001111
    let CoinTypeAll       = AliasRealExInSystem ||| AliasRealExInFlow ||| AliasRealInFlow ||| AliasCallInFlow ||| AliasCallInReal ||| CallInReal ||| CallInFlow ||| RealExSystem ||| RealExFlow                    // 0b1111111110
    let CallTypeAll       =                                                                                                           CallInReal ||| CallInFlow ||| RealExSystem                                   // 0b0000011100
    let RealNIndirectReal = AliasRealExInSystem ||| AliasRealExInFlow ||| AliasRealInFlow                                                                                        ||| RealExFlow ||| RealInFlow     // 0b1110000011
    let VertexAll         = AliasRealExInSystem ||| AliasRealExInFlow ||| AliasRealInFlow ||| AliasCallInFlow ||| AliasCallInReal ||| CallInReal ||| CallInFlow ||| RealExSystem ||| RealExFlow ||| RealInFlow     // 0b1111111111

