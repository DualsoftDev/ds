[<AutoOpen>]
module Engine.CodeGenCPU.ConvertPort

open Engine.CodeGenCPU
open Engine.Core
open Engine.Common.FS
open System.Linq

//Port 처리 Set 공용 함수
let private getSetBits(v:VertexManager) (rse:SRE) (convert:ConvertType) =
    let shareds =  
        match convert with
        | ConvertType.RealPure -> v.GetSharedReal() 
        | ConvertType.CallPure -> v.GetSharedCall() 
        | _ ->  failwith "Error"

    let setBits =  
        match rse with
        |Start -> (shareds |> startTags ) @ [v.ST;v.SF]
        |Reset -> (shareds |> resetTags ) @ [v.ET;v.RF]
        |End   -> (shareds |> endTags   ) @ [v.ET;v.EF]    

    setBits.Cast<Tag<bool>>() |> toAnd
//Port 처리 Rst 공용 함수
let private getRstBits(v:VertexManager) = v.OFF.Expr
    

type VertexManager with
    
    member v.P1_RealStartPort(): CommentedStatement =
        let sets = getSetBits v SRE.Start RealPure
        let rsts = getRstBits v
         
        (sets, rsts) --| (v.SP, "P1")

    member v.P2_RealResetPort(): CommentedStatement =
        let sets = getSetBits v SRE.Reset RealPure
        let rsts = getRstBits v
         
        (sets, rsts) --| (v.RP, "P2")

    member v.P3_RealEndPort(): CommentedStatement =
        let sets = getSetBits v SRE.End RealPure
        let rsts = getRstBits v
         
        (sets, rsts) --| (v.EP, "P3")

    member v.P4_CallStartPort(): CommentedStatement =
        let sets = getSetBits v SRE.Start CallPure
        let rsts = getRstBits v
         
        (sets, rsts) --| (v.SP, "P4")

    member v.P5_CallResetPort(): CommentedStatement =
        let sets = getSetBits v SRE.Reset CallPure
        let rsts = getRstBits v
         
        (sets, rsts) --| (v.RP, "P5")

    member v.P6_CallEndPort(): CommentedStatement =
        let sets = getSetBits v SRE.End CallPure
        let rsts = getRstBits v
         
        (sets, rsts) --| (v.EP, "P6")
