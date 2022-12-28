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
    |RealPure            = 0b00000001  
    |RealExPure          = 0b00000010  
    |CallPure            = 0b00000100  
    |AliasPure           = 0b00001000  
    |AliasForCall        = 0b00100000  
    |AliasForReal        = 0b01000000  
    |AliasForRealEx      = 0b10000000  
    |VertexAll           = 0b11111111  
    //RC      //Real, Call as RC
    //RCA     //Real, Call, Alias(Call) as RCA
    //RRA     //Real, RealExF, Alias(Real) as RRA
    //CA      //Call, Alias(Call) as CA 
    //V       //Real, RealExF, Call, Alias as V

    let IsSpec (v:Vertex) (vaild:ConvertType) = 
        let isVaildVertex =
            match v with
            | :? Real   -> vaild.HasFlag(RealPure)
            | :? RealEx -> vaild.HasFlag(RealExPure) 
            | :? Call   -> vaild.HasFlag(CallPure)
            | :? Alias as a  -> 
                match a.TargetWrapper with
                | DuAliasTargetReal ar   -> vaild.HasFlag(AliasForReal)
                | DuAliasTargetCall ac   -> vaild.HasFlag(AliasForCall)
                | DuAliasTargetRealEx ao -> vaild.HasFlag(AliasForRealEx)
            |_ -> failwith "Error"

        isVaildVertex
        //if not <| isVaildVertex 
        //then failwith $"{v.Name} can't applies to [{vaild}] case"
