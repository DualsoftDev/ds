namespace Engine.CodeGenCPU

open Engine.Core
open System
open Engine.Common.FS

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
    |RealInFlow          = 0b00000001  
    |RealExFlow          = 0b00000010  
    |CallInFlow          = 0b00000100  
    |CallInReal          = 0b00001000  
    |AliasCallInReal     = 0b00010000  
    |AliasCallInFlow     = 0b00100000  
    |AliasRealInFlow     = 0b01000000  
    |AliasRealExInFlow   = 0b10000000  

    |InFlowWithoutReal   = 0b11100110 
    |InFlowAll           = 0b11100111 
    |CoinTypeAll         = 0b11111110 
    |CallTypeAll         = 0b00001100 
    |RealNIndirectReal   = 0b11000011 
    |VertexAll           = 0b11111111 
   
    let IsSpec (v:Vertex) (vaild:ConvertType) = 
        let isVaildVertex =
            match v with
            | :? Real   -> vaild.HasFlag(RealInFlow)
            | :? RealEx -> vaild.HasFlag(RealExFlow)
            | :? Call as c  ->
                match c.Parent with
                | DuParentFlow f-> vaild.HasFlag(CallInFlow)
                | DuParentReal r-> vaild.HasFlag(CallInReal)

            | :? Alias as a  ->
                 match a.Parent with
                 | DuParentFlow f->
                     match a.TargetWrapper with
                     |  DuAliasTargetReal   ar -> vaild.HasFlag(AliasRealInFlow)
                     |  DuAliasTargetRealEx ao -> vaild.HasFlag(AliasRealExInFlow)
                     |  DuAliasTargetCall   ac -> vaild.HasFlag(AliasCallInFlow)
                 | DuParentReal r->
                     match a.TargetWrapper with
                     | DuAliasTargetReal   ar -> failwithlog "Error IsSpec"
                     | DuAliasTargetRealEx ao -> failwithlog "Error IsSpec"
                     | DuAliasTargetCall   ac -> vaild.HasFlag(AliasCallInReal)
            |_ -> failwithlog "Error"

        isVaildVertex
