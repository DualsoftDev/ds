[<AutoOpen>]
module Engine.CodeGenCPU.ConvertPort

open Engine.CodeGenCPU
open Engine.Core
open Engine.Common.FS
open System.Linq

//Port 처리 Set 공용 함수
let private getSetBits(v:VertexManager) (rse:SREType) (convert:ConvertType) =
    let shareds =  
        if convert =  ConvertType.RealInFlow 
        then v.GetSharedReal() 
        elif convert = (ConvertType.CallInFlow  |||  ConvertType.CallInReal)
        then v.GetSharedCall() 
        else failwith "Error"

    let setBits =  
        match rse with
        |Start -> (shareds.STs()) @ [v.ST;v.SF]
        |Reset -> (shareds.RTs()) @ [v.RT;v.RF]
        |End   -> (shareds.ETs()) @ [v.ET;v.EF]    

    setBits.Cast<Tag<bool>>() |> toAnd
//Port 처리 Rst 공용 함수
let private getRstBits(v:VertexManager) = v.System._on.Expr
    

type VertexManager with
    
    member v.P1_RealStartPort(): CommentedStatement =
        let sets = getSetBits v SREType.Start RealInFlow
        let rsts = getRstBits v
         
        (sets, rsts) --| (v.SP, "P1")

    member v.P2_RealResetPort(): CommentedStatement =
        let sets = getSetBits v SREType.Reset RealInFlow
        let rsts = getRstBits v
         
        (sets, rsts) --| (v.RP, "P2")

    member v.P3_RealEndPort(): CommentedStatement =
        let sets = getSetBits v SREType.End RealInFlow
        let rsts = getRstBits v
         
        (sets, rsts) --| (v.EP, "P3")

    member v.P4_CallStartPort(): CommentedStatement =
        let sets = getSetBits v SREType.Start (CallInFlow  ||| CallInReal)
        let rsts = getRstBits v
         
        (sets, rsts) --| (v.SP, "P4")

    member v.P5_CallResetPort(): CommentedStatement =
        let sets = getSetBits v SREType.Reset (CallInFlow  ||| CallInReal)
        let rsts = getRstBits v
         
        (sets, rsts) --| (v.RP, "P5")

    member v.P6_CallEndPort(): CommentedStatement =
        let sets = getSetBits v SREType.End (CallInFlow  ||| CallInReal)
        let rsts = getRstBits v
         
        (sets, rsts) --| (v.EP, "P6")
