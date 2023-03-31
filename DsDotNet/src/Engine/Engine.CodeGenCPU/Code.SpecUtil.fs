namespace Engine.CodeGenCPU

open System

[<AutoOpen>]
module CodeSpecUtil =

    [<AutoOpen>]
    type SREType =
        | Start
        | Reset
        | End

    [<AutoOpen>]
    type ConvertAlias =
        | AliasTure         //Alias 만 적용
        | AliasFalse        //Alias 미 적용
        | AliasNotCare      //Alias 상관없음

    [<Flags>]
    [<AutoOpen>]
    type ConvertType =
        | RealInFlow          = 0b000001
        | RealExFlow          = 0b000010
        | RealExSystem        = 0b000100
        | CallInFlow          = 0b001000
        | CallInReal          = 0b010000

    let VertexAll         =  CallInReal ||| CallInFlow ||| RealExSystem ||| RealExFlow ||| RealInFlow     // 0b11111

