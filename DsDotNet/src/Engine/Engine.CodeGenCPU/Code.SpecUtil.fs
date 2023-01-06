namespace Engine.CodeGenCPU

open System.Linq
open System.Runtime.CompilerServices
open Engine.Core
open System
open Engine.Common.FS

[<AutoOpen>]
module CodeSpecUtil =

    [<AutoOpen>]
    type SREType = 
    |Start
    |Reset
    |End
    
    [<Flags>]
    [<AutoOpen>]
    type ConvertType = 
    |RealInFlow          = 0b0000000001  
    |RealExFlow          = 0b0000000010  
    |CallInFlow          = 0b0000000100  
    |CallInReal          = 0b0000001000  
    |AliasCallInReal     = 0b0000010000  
    |AliasRealInReal     = 0b0000100000  
    |AliasRealExInReal   = 0b0001000000 
    |AliasCallInFlow     = 0b0010000000  
    |AliasRealInFlow     = 0b0100000000  
    |AliasRealExInFlow   = 0b1000000000  
    |CoinTypeAll         = 0b0010011100 
    |RealTypeAll         = 0b0000000011 
    |VertexAll           = 0b1111111111 
   
    let IsSpec (v:Vertex) (vaild:ConvertType) = 
        let isVaildVertex =
            match v with
            | :? Real   -> vaild.HasFlag(RealInFlow)
            | :? RealEx -> vaild.HasFlag(RealExFlow) 
            | :? Call as c  -> 
                match c.Parent with
                |DuParentFlow f-> vaild.HasFlag(CallInFlow)
                |DuParentReal r-> vaild.HasFlag(CallInReal)

            | :? Alias as a  -> 
                 match a.Parent with
                 |DuParentFlow f-> 
                     match a.TargetWrapper with
                     | DuAliasTargetReal ar   -> vaild.HasFlag(AliasRealInFlow)
                     | DuAliasTargetRealEx ao -> vaild.HasFlag(AliasRealExInFlow)
                     | DuAliasTargetCall ac   -> vaild.HasFlag(AliasCallInFlow)
                 |DuParentReal r-> 
                     match a.TargetWrapper with
                     | DuAliasTargetReal ar   -> vaild.HasFlag(AliasRealInReal)
                     | DuAliasTargetRealEx ao -> vaild.HasFlag(AliasRealExInReal)
                     | DuAliasTargetCall ac   -> vaild.HasFlag(AliasCallInReal)
            |_ -> failwith "Error"

        isVaildVertex
