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
    |RealInFlow          = 0b000000001  
    |RealExFlow          = 0b000000010  
    |CallInFlow          = 0b000000100  
    |CallInReal          = 0b000000100  
    |AliasCallInReal     = 0b000001000  
    |AliasRealInReal     = 0b000010000  
    |AliasRealExInReal   = 0b000100000 
    |AliasCallInFlow     = 0b001000000  
    |AliasRealInFlow     = 0b010000000  
    |AliasRealExInFlow   = 0b100000000  
    |VertexAll           = 0b111111111  
   
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
