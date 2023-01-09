[<AutoOpen>]
module Engine.CodeGenCPU.ConvertPort

open Engine.CodeGenCPU
open Engine.Core
open Engine.Common.FS
open System.Linq

//Port 처리 Set 공용 함수
let private getSetBits(v:VertexManager) (rse:SREType) =
    let shareds =  v.GetSharedReal()
    let setBits =  
        match rse with
        |Start -> (shareds.STs()) @ [v.ST;v.SF]
        |Reset -> (shareds.RTs()) @ [v.RT;v.RF]
        |End   -> (shareds.ETs()) @ [v.ET;v.EF]    

    setBits.ToOr()

type VertexManager with
    
    member v.P1_RealStartPort(): CommentedStatement =
        let sets = getSetBits v SREType.Start 
        let rsts = v.System._off.Expr
         
        (sets, rsts) --| (v.SP, "P1")

    member v.P2_RealResetPort(): CommentedStatement =
        let sets = getSetBits v SREType.Reset 
        let rsts = v.System._off.Expr
         
        (sets, rsts) --| (v.RP, "P2")

    member v.P3_RealEndPort(): CommentedStatement =
        let sets = getSetBits v SREType.End 
        let rsts = v.System._off.Expr
         
        (sets, rsts) --| (v.EP, "P3")

    member v.P4_CallStartPort(): CommentedStatement =
        let sets = v.ST.Expr <||> v.SF.Expr
        let rsts = v.System._off.Expr
         
        (sets, rsts) --| (v.SP, "P4")

    member v.P5_CallResetPort(): CommentedStatement =
        let sets = v.RT.Expr <||> v.RF.Expr
        let rsts = v.System._off.Expr
         
        (sets, rsts) --| (v.RP, "P5")

    member v.P6_CallEndPort(): CommentedStatement =
        let sets = v.ET.Expr <||> v.EF.Expr
        let rsts = v.System._off.Expr
         
        (sets, rsts) --| (v.EP, "P6")
